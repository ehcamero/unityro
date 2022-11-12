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
    private Texture2D MapThumbTexture;
    private Texture2D PlayerIndicatorTexture;
    #endregion

    #region GameObjects fields
    private RawImage mapThumb;
    private TextMeshProUGUI mapCoordinate;
    private TextMeshProUGUI mapName;
    private RawImage playerArrow;
    private RectMask2D maskMiniMap;
    private Button buttonMinus;
    private Button buttonPlus;
    #endregion

    private string CurrentMap;
    private int CurrentZoom = 1;

    void Awake()
    {
        Session.OnMapChanged += OnMapChanged;
        Session.OnPositionChanged += OnPositionChangedUpdateCoordinates;
    }

    private void OnDestroy()
    {
        Session.OnMapChanged -= OnMapChanged;
        Session.OnPositionChanged -= OnPositionChangedUpdateCoordinates;
    }

    async void Start() {
        Transform miniMapBase = transform.Find("Minimap");
        maskMiniMap = miniMapBase.Find("Mask").GetComponent<RectMask2D>();
        mapCoordinate = miniMapBase.Find("MapCoordinate").GetComponent<TextMeshProUGUI>();
        mapName = miniMapBase.Find("MapName").GetComponent<TextMeshProUGUI>();

        mapThumb = maskMiniMap.transform.Find("MinimapImage").GetComponent<RawImage>();
        playerArrow = maskMiniMap.transform.Find("PlayerArrowImage").GetComponent<RawImage>();

        buttonMinus = miniMapBase.Find("ButtonMinus").GetComponent<Button>();
        buttonPlus = miniMapBase.Find("ButtonPlus").GetComponent<Button>();

        buttonMinus.onClick.RemoveAllListeners();
        buttonMinus.onClick.AddListener(OnClickButtonMinus);
        buttonPlus.onClick.RemoveAllListeners();
        buttonPlus.onClick.AddListener(OnClickButtonPlus);

        

        PlayerIndicatorTexture = await Addressables.LoadAssetAsync<Texture2D>($"{DBManager.INTERFACE_PATH}map/map_arrow.png").Task;
    }

    private void OnPositionChangedUpdateCoordinates(short xPos, short yPos)
    {
        SetCoordinateMiniMap(xPos, yPos);
    }
    private async void OnMapChanged(string mapName) {
        CurrentMap = Path.GetFileNameWithoutExtension(mapName);
        MapThumbTexture = await Addressables.LoadAssetAsync<Texture2D>($"{DBManager.INTERFACE_PATH}map/{CurrentMap}.png").Task;

        if (MapThumbTexture == null) {
            return;
        }

        mapThumb.texture = MapThumbTexture;
        var size = CalculateNewSize(MapThumbTexture.width, MapThumbTexture.height, 128, 128);
        (transform as RectTransform).sizeDelta = size;
    }

    private void Update() {
        if (CurrentMap != null && MapThumbTexture == null) {
            OnMapChanged(CurrentMap);
        }
    }

    private Vector2 CalculateNewSize(int srcWidth, int srcHeight, int maxWidth, int maxHeight) {
        var ratio = Mathf.Min((float) maxWidth / (float) srcWidth, (float) maxHeight / (float) srcHeight);
        return new Vector2(srcWidth * ratio, srcHeight * ratio);
    }

    private void SetCoordinateMiniMap(short xPos, short yPos)
    {
        mapCoordinate.text = $"{xPos} {yPos}";
    }

}
