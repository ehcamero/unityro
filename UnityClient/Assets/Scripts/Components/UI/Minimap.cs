using ROIO;
using System;
using System.Drawing;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class Minimap : MonoBehaviour 
{
    #region Enums
    private enum MiniMapComponents
    {
        MINIMAP,
        PLAYER_ARROW
    }
    #endregion

    #region Textures fields
    private Texture2D mapThumbTexture;
    private Texture2D playerIndicatorTexture;
    #endregion

    #region GameObjects fields
    private RawImage mapThumb;
    private TextMeshProUGUI mapCoordinate;
    private TextMeshProUGUI mapName;
    private RawImage playerArrow;
    private RectTransform minimapMask;
    private Button buttonMinus;
    private Button buttonPlus;
    #endregion

    #region SerializedFields
    [SerializeField]
    private int currentZoomTest = 0;
    [SerializeField]
    private float offSetX = 0f;
    [SerializeField]
    private float offSetY = 0f;
    [SerializeField]
    private float centerPointX = 0;
    [SerializeField]
    private float centerPointY = 0;
    [SerializeField]
    private float widthTest = 128;
    [SerializeField]
    private float heightTest = 128;
    #endregion

    #region Singletons
    Entity player;
    #endregion

    #region Constants
    private const int DEFAULT_ZOOM_INDEX = 0;
    #endregion

    private Vector2 mapAndMiniMapScaleFactors;
    private Rect miniMapOriginalRect;
    private Vector2 currentPlayerCoordinates;
    private Vector2 currentPlayerArrowCoordinates;
    private Vector2 lastPlayerCoordinates;
    private Vector2 lastPlayerArrowCoordinates;
    private Vector2 playerDelta;
    private Vector2 playerArrowDelta;
    private Vector2 centerPoint;
    private bool isZoomApplied = false;
    private bool isFirstPlayerArrowCoordinates = true;
    private int currentZoom;
    private float[] zoomValues =
    {
        1.25f,
        1.45f,
        1.75f,
        2.0f,
        2.3f
    };


    private string currentMap;
    private float miniMapWidth = 0f;
    private float miniMapHeight = 0f;
    private float maskMiniMapWidth = 0f;
    private float maskMiniMapHeight = 0f;
    private uint realMapWidth = 0;
    private uint realMapHeight = 0;
    

    void Awake()
    {
        Session.OnMapChanged += OnMapChanged;
    }

    private void LateUpdate()
    {
        if(Session.CurrentSession == null) return;
        TranslatePlayerArrow();
    }

    private void OnDestroy()
    {
        Session.OnMapChanged -= OnMapChanged;
    }

    async void Start() {
        Transform miniMapBase = this.transform;

        minimapMask = miniMapBase.Find("MinimapMask").GetComponent<RectTransform>();
        mapCoordinate = miniMapBase.Find("MapCoordinate").GetComponent<TextMeshProUGUI>();
        mapName = miniMapBase.Find("MapName").GetComponent<TextMeshProUGUI>();

        mapThumb = minimapMask.transform.Find("MinimapContent").GetComponent<RawImage>();
        playerArrow = minimapMask.transform.Find("MinimapPlayerArrow").GetComponent<RawImage>();

        buttonMinus = miniMapBase.Find("ButtonMinus").GetComponent<Button>();
        buttonPlus = miniMapBase.Find("ButtonPlus").GetComponent<Button>();

        buttonMinus.onClick.RemoveAllListeners();
        buttonMinus.onClick.AddListener(OnClickButtonMinus);
        buttonPlus.onClick.RemoveAllListeners();
        buttonPlus.onClick.AddListener(OnClickButtonPlus);

        // Get player indicator on minimap texture
        playerIndicatorTexture = await Addressables.LoadAssetAsync<Texture2D>($"{DBManager.INTERFACE_PATH}map/map_arrow.png").Task;
        playerArrow.texture = playerIndicatorTexture;

        // Get minimap texture and name for the first time
        mapThumbTexture = await Addressables.LoadAssetAsync<Texture2D>($"{DBManager.INTERFACE_PATH}map/{Session.CurrentSession.CurrentMap}.png").Task;
        mapThumb.texture = mapThumbTexture;
        SetMapName(Session.CurrentSession.CurrentMap);
        miniMapOriginalRect = mapThumb.rectTransform.rect;

        // Get entity player
        player = Session.CurrentSession.Entity as Entity;
    }

    #region Events implementation
    private void OnClickButtonMinus()
    {
        MiniMapZoom(false);
    }
    private void OnClickButtonPlus()
    {
        MiniMapZoom(true);
    }

    private async void OnMapChanged(string mapName)
    {
        // Get map texture
        currentMap = Path.GetFileNameWithoutExtension(mapName);
        mapThumbTexture = await Addressables.LoadAssetAsync<Texture2D>($"{DBManager.INTERFACE_PATH}map/{currentMap}.png").Task;

        if (mapThumbTexture == null) {
            Debug.LogError("Map texture is null");
            return;
        }
        mapThumb.texture = mapThumbTexture;
        SetMapName(currentMap);
        GetCurrentPlayerCoordinates();
    }
    #endregion

    private void GetCurrentPlayerCoordinates()
    {
        currentPlayerArrowCoordinates.x = currentPlayerCoordinates.x = (int) Math.Truncate(player.transform.position.x);
        currentPlayerArrowCoordinates.y = currentPlayerCoordinates.y = (int) Math.Truncate(player.transform.position.z);
    }

    private void MiniMapZoom(bool isPlus)
    {
        if (isPlus)
        {
            ZoomOut();
        }
        else
        {
            ZoomIn();
        }
    }

    private void ZoomOut()
    {
        if (currentZoom >= zoomValues.Length)
        {
            currentZoom = zoomValues.Length - 1;
        } else
        {
            mapThumb.rectTransform.sizeDelta = new Vector2(miniMapOriginalRect.width * zoomValues[currentZoom], miniMapOriginalRect.height * zoomValues[currentZoom]);
            currentZoom++;
        }

        if (currentZoom > DEFAULT_ZOOM_INDEX)
            isZoomApplied = true;
    }

    private void ZoomIn()
    {
        currentZoom--;
        if(currentZoom <= DEFAULT_ZOOM_INDEX)
        {
            currentZoom = DEFAULT_ZOOM_INDEX;
            mapThumb.rectTransform.sizeDelta = new Vector2(miniMapOriginalRect.width, miniMapOriginalRect.height);
        } else
        {
            mapThumb.rectTransform.sizeDelta = new Vector2(miniMapOriginalRect.width * zoomValues[currentZoom], miniMapOriginalRect.height * zoomValues[currentZoom]);
        }

        if (currentZoom == DEFAULT_ZOOM_INDEX)
            isZoomApplied = false;
    }

    private void GetMapAndMiniMapScaleFactors()
    {
        mapAndMiniMapScaleFactors.x = mapThumb.rectTransform.rect.x < MapRenderer.width ?
            mapThumb.rectTransform.rect.x / MapRenderer.width : MapRenderer.width / mapThumb.rectTransform.rect.x;

        mapAndMiniMapScaleFactors.y = mapThumb.rectTransform.rect.y < MapRenderer.height ?
            mapThumb.rectTransform.rect.y / MapRenderer.height : MapRenderer.height / mapThumb.rectTransform.rect.y;
    }

    private Vector2 GetPlayerDelta()
    {
        Vector2 auxVector = Vector2.zero;

        auxVector.x = currentPlayerCoordinates.x - lastPlayerCoordinates.x;
        auxVector.y = currentPlayerCoordinates.y - lastPlayerCoordinates.y;

        Debug.Log($"Delta player X: {auxVector.x} Delta Y player: {auxVector.y}");

        return auxVector;
    }

    private Vector2 GetPlayerArrowDelta()
    {
        Vector2 auxVector = Vector2.zero;

        auxVector.x = currentPlayerArrowCoordinates.x - lastPlayerArrowCoordinates.x;
        auxVector.y = currentPlayerArrowCoordinates.y - lastPlayerArrowCoordinates.y;

        Debug.Log($"Delta player arrow X: {auxVector.x} Delta player arrow Y: {auxVector.y}");

        return auxVector;
    }

    private float CalculateNewXValue(Vector2 delta, MiniMapComponents miniMapComponent)
    {
        switch(miniMapComponent)
        {
            case MiniMapComponents.MINIMAP:
                return mapThumb.transform.localPosition.x + (Math.Abs(mapAndMiniMapScaleFactors.x) * delta.x);
            case MiniMapComponents.PLAYER_ARROW:
                return playerArrow.transform.localPosition.x + (Math.Abs(mapAndMiniMapScaleFactors.x) * delta.x);
            default:
                return mapThumb.transform.localPosition.x;
        }
    }

    private float CalculateNewYValue(Vector2 delta, MiniMapComponents miniMapComponent)
    {
        switch (miniMapComponent) {
            case MiniMapComponents.MINIMAP:
                return mapThumb.transform.localPosition.y + (Math.Abs(mapAndMiniMapScaleFactors.y) * delta.y);
            case MiniMapComponents.PLAYER_ARROW:
                return playerArrow.transform.localPosition.y + (Math.Abs(mapAndMiniMapScaleFactors.y) * delta.y);
            default:
                return mapThumb.transform.localPosition.y;
        }
    }

    private void TranslateMiniMap()
    {
        if (lastPlayerCoordinates == currentPlayerCoordinates) return;

        playerDelta = GetPlayerDelta();

        if (isZoomApplied)
        {
            mapThumb.transform.localPosition = new Vector3(CalculateNewXValue(playerDelta, MiniMapComponents.MINIMAP), CalculateNewYValue(playerDelta, MiniMapComponents.MINIMAP), 0);
        }
        
        lastPlayerCoordinates = currentPlayerCoordinates;
    }

    private void TranslatePlayerArrow()
    {
        UpadatePlayerArrowDirection(player.Direction);

        if (lastPlayerArrowCoordinates == currentPlayerArrowCoordinates) return;

        playerArrowDelta = GetPlayerArrowDelta();

        if (!isFirstPlayerArrowCoordinates) {
            playerArrow.transform.localPosition = new Vector3(CalculateNewXValue(playerArrowDelta, MiniMapComponents.PLAYER_ARROW), CalculateNewYValue(playerArrowDelta, MiniMapComponents.PLAYER_ARROW), 0);
            Debug.Log($"Current player arrow pos X: {playerArrow.transform.localPosition.x} Current player arrow pos Y: {playerArrow.transform.localPosition.y}");
        }

        if (isFirstPlayerArrowCoordinates)
        {
            playerArrow.transform.localPosition = new Vector3(-centerPoint.x, -centerPoint.y, 0);
            playerArrow.transform.localPosition = new Vector3(CalculateNewXValue(playerArrowDelta, MiniMapComponents.PLAYER_ARROW), CalculateNewYValue(playerArrowDelta, MiniMapComponents.PLAYER_ARROW), 0);
            isFirstPlayerArrowCoordinates = false;
            Debug.Log($"Initial player arrow pos X: {playerArrow.transform.localPosition.x} Initial player arrow pos Y: {playerArrow.transform.localPosition.y}");
        }

        lastPlayerArrowCoordinates = currentPlayerArrowCoordinates;
    }

    private void UpadatePlayerArrowDirection(Direction playerDirection)
    {
        switch (playerDirection) {
            case Direction.North:
                playerArrow.transform.eulerAngles = new Vector3(0, 0, 0);
                return;
            case Direction.NorthEast:
                playerArrow.transform.eulerAngles = new Vector3(0, 0, 315);
                return;
            case Direction.East:
                playerArrow.transform.eulerAngles = new Vector3(0, 0, 270);
                return;
            case Direction.SouthEast:
                playerArrow.transform.eulerAngles = new Vector3(0, 0, 225);
                return;
            case Direction.South:
                playerArrow.transform.eulerAngles = new Vector3(0, 0, 180);
                return;
            case Direction.SouthWest:
                playerArrow.transform.eulerAngles = new Vector3(0, 0, 135);
                return;
            case Direction.West:
                playerArrow.transform.eulerAngles = new Vector3(0, 0, 90);
                return;
            case Direction.NorthWest:
                playerArrow.transform.eulerAngles = new Vector3(0, 0, 45);
                return;
            default:
                playerArrow.transform.eulerAngles = new Vector3(0, 0, 0);
                return;
        }
    }

    private void GetCenterPoint()
    {
        centerPoint.x = mapThumb.rectTransform.rect.width / 2;
        centerPoint.y = mapThumb.rectTransform.rect.height / 2;
    }

    private void Update()
    {
        if (currentMap != null && mapThumbTexture == null)
        {
            OnMapChanged(currentMap);
        }

        if(MapRenderer.width != 0)
        {
            GetMapAndMiniMapScaleFactors();
        }

        GetCurrentPlayerCoordinates();
        GetCenterPoint();
        UpdateCoordinates();
        TranslateMiniMap();
    }

    private void SetMapName(string currentMapName)
    {
        mapName.text = currentMapName;
    }

    private void UpdateCoordinates()
    {
        mapCoordinate.text = $"{Math.Truncate(player.transform.position.x)} {Math.Truncate(player.transform.position.z)}";
    }

    /*
    private void SetCoordinateMiniMap(short xPos, short yPos)
    {
        mapCoordinate.text = $"{xPos} {yPos}";
    }

    private void UpdatePlayerArrow()
    {
        // Getting the player avatar
        Entity player = Session.CurrentSession.Entity as Entity;

        if (player == null) return;

        int convertedMiniMapPointX = 0, convertedMiniMapPointY = 0;
        ConvertMapCoordinatesToMiniMapCoordinates((int)player.transform.position.x, (int)player.transform.position.y, ref convertedMiniMapPointX, ref convertedMiniMapPointY);

        // Update the player arrow
        playerArrow.rectTransform.anchoredPosition3D = new Vector3(ConvertRealPixelPointX(convertedMiniMapPointX), ConvertRealPixelPointY(convertedMiniMapPointY));

        // Update the player arrow rotation
        playerArrow.rectTransform.localRotation = Quaternion.Euler(0, 0, 120);

        int convertedMiniMapPointWidth = 0, convertedMiniMapPointHeight = 0;
        ConvertMapCoordinatesToMiniMapCoordinates((int) player.transform.position.x, (int) player.transform.position.y, ref convertedMiniMapPointWidth, ref convertedMiniMapPointHeight);
        SetCenterPoint(convertedMiniMapPointWidth, convertedMiniMapPointHeight);
        UpdateMiniMapOffset(false);
    }

    private void ConvertMapCoordinatesToMiniMapCoordinates(int playerPositionX, int playerPositionY, ref int refMiniMapX, ref int refMiniMapY)
    {
        refMiniMapX = (int)(playerPositionX * miniMapWidth / realMapWidth);
        refMiniMapY = (int) (playerPositionY * miniMapHeight / realMapHeight);
    }

    private float ConvertRealPixelPointX(float convertedMiniMapPointX)
    {
        return convertedMiniMapPointX * zoomValues[currentZoom] + offSetX;
    }

    private float ConvertRealPixelPointY(float convertedMiniMapPointY) {
        return convertedMiniMapPointY * zoomValues[currentZoom] + offSetY;
    }

    private void UpdateMiniMapOffset(bool isUpdateScale)
    {
        float scale = zoomValues[currentZoom];

        if (isUpdateScale)
        {
            mapThumb.rectTransform.localScale = new Vector2(scale, scale);
        }

        mapThumb.rectTransform.anchoredPosition3D = new Vector3(offSetX, offSetY, 0f);
    }

    private void SetCenterPoint(int x, int y)
    {
        if(centerPointX == x && centerPointY == y)
        {
            return;
        }

        centerPointX = x;
        centerPointY = y;
        offSetX = (maskMiniMapWidth / 2) - x * zoomValues[currentZoom];
        offSetY = (maskMiniMapHeight / 2) - y * zoomValues[currentZoom];

        offSetX = Math.Min(offSetX, 0);
        offSetY = Math.Min(offSetY, 0);
        offSetX = Math.Max(offSetX, -miniMapWidth * zoomValues[currentZoom] + maskMiniMapWidth);
        offSetX = Math.Max(offSetY, -miniMapHeight * zoomValues[currentZoom] + maskMiniMapHeight);
    }
    */
}
