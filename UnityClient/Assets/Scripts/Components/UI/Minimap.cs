using ROIO;
using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class Minimap : MonoBehaviour 
{
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
    private int currentZoom = 0;
    [SerializeField]
    private float offSetX = 0f;
    [SerializeField]
    private float offSetY = 0f;
    [SerializeField]
    private float centerPointX = 0;
    [SerializeField]
    private float centerPointY = 0;
    #endregion

    #region Singletons
    Entity player;
    #endregion

    private string currentMap;
    private float miniMapWidth = 0f;
    private float miniMapHeight = 0f;
    private float maskMiniMapWidth = 0f;
    private float maskMiniMapHeight = 0f;
    private uint realMapWidth = 0;
    private uint realMapHeight = 0;
    private float[] zoomValues =
    {
        0.25f,
        0.32f,
        0.425f,
        0.75f,
        1.0f
    };

    void Awake()
    {
        Session.OnMapChanged += OnMapChanged;
    }

    private void LateUpdate()
    {
        /*
        if(Session.CurrentSession == null) return;
        UpdatePlayerArrow();
        */
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

        /*
        buttonMinus.onClick.RemoveAllListeners();
        buttonMinus.onClick.AddListener(OnClickButtonMinus);
        buttonPlus.onClick.RemoveAllListeners();
        buttonPlus.onClick.AddListener(OnClickButtonPlus);
        */

        // Get player indicator on minimap texture
        playerIndicatorTexture = await Addressables.LoadAssetAsync<Texture2D>($"{DBManager.INTERFACE_PATH}map/map_arrow.png").Task;
        playerArrow.texture = playerIndicatorTexture;

        // Get minimap texture and name for the first time
        mapThumbTexture = await Addressables.LoadAssetAsync<Texture2D>($"{DBManager.INTERFACE_PATH}map/{Session.CurrentSession.CurrentMap}.png").Task;
        mapThumb.texture = mapThumbTexture;
        SetMapName(Session.CurrentSession.CurrentMap);

        // Get entity player
        player = Session.CurrentSession.Entity as Entity;
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

        /*
        // Get player avatar
        Entity player = Session.CurrentSession.Entity as Entity;

        if(player == null)
        {
            Debug.LogError("Player entity is null");
            return;
        }

        

        // Reset zoom
        currentZoom = 0;

        miniMapWidth = mapThumb.rectTransform.rect.width;
        miniMapHeight = mapThumb.rectTransform.rect.height;

        maskMiniMapWidth = maskMiniMap.rectTransform.sizeDelta.x;
        maskMiniMapHeight = maskMiniMap.rectTransform.sizeDelta.y;

        realMapWidth = MapRenderer.width;
        realMapHeight = MapRenderer.height;

        SetMapName(currentMap);
        SetCoordinateMiniMap((short) player.transform.position.x, (short) player.transform.position.y);
        UpdateMiniMapOffset(true);

        /*
        var size = CalculateNewSize(mapThumbTexture.width, mapThumbTexture.height, 128, 128);
        (transform as RectTransform).sizeDelta = size;
        */
    }

    private void Update()
    {
        if (currentMap != null && mapThumbTexture == null)
        {
            OnMapChanged(currentMap);
        }

        UpdateCoordinates();
    }

    /*
    private Vector2 CalculateNewSize(int srcWidth, int srcHeight, int maxWidth, int maxHeight) {
        var ratio = Mathf.Min((float) maxWidth / (float) srcWidth, (float) maxHeight / (float) srcHeight);
        return new Vector2(srcWidth * ratio, srcHeight * ratio);
    }
    */

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
