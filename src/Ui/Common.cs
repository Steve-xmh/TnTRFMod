using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TnTRFMod.Ui;

public class Common
{
    // 游戏的设计屏幕分辨率
    public const int ScreenWidth = 1920;
    public const int ScreenHeight = 1080;
    private static FontTMPManager? _fontMgr;
    private static GameObject? _drawCanvasForScene;
    private static CanvasGroup? _drawCanvasForSceneCanvasGroup;
    private static GameObject? _drawCanvasForSceneNoDestroy;
    private static CanvasGroup? _drawCanvasForSceneNoDestroyCanvasGroup;
    private static ControllerManager? _controllerManager;
    private static bool inited;

    public static void Init()
    {
        if (_drawCanvasForSceneNoDestroy != null) return;
        _drawCanvasForSceneNoDestroy = new GameObject("CanvasForTnTRFModNoDestroy");
        var transform = _drawCanvasForSceneNoDestroy.AddComponent<RectTransform>();
        NormalizeCanvasRect(transform);
        Object.DontDestroyOnLoad(_drawCanvasForSceneNoDestroy);
        _drawCanvasForSceneNoDestroyCanvasGroup = _drawCanvasForSceneNoDestroy.AddComponent<CanvasGroup>();
        _drawCanvasForSceneNoDestroy.hideFlags = HideFlags.HideAndDontSave;

        var canvas = _drawCanvasForSceneNoDestroy.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = short.MaxValue;

        var scaler = _drawCanvasForSceneNoDestroy.AddComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(ScreenWidth, ScreenHeight);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        _drawCanvasForSceneNoDestroy.AddComponent<GraphicRaycaster>();
        _drawCanvasForSceneNoDestroy.layer = LayerMask.NameToLayer("UI");
        _drawCanvasForSceneNoDestroy.SetActive(true);
    }

    public static void InitLocal()
    {
        if (_drawCanvasForScene != null) return;
        _drawCanvasForScene = new GameObject("CanvasForTnTRFMod");
        var transform = _drawCanvasForScene.AddComponent<RectTransform>();
        NormalizeCanvasRect(transform);
        var canvas = _drawCanvasForScene.AddComponent<Canvas>();
        _drawCanvasForSceneCanvasGroup = _drawCanvasForScene.AddComponent<CanvasGroup>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;
        var scaler = _drawCanvasForScene.AddComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(ScreenWidth, ScreenHeight);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        _drawCanvasForScene.AddComponent<GraphicRaycaster>();
        _drawCanvasForScene.layer = LayerMask.NameToLayer("UI");
    }

    private static void NormalizeCanvasRect(RectTransform transform)
    {
        transform.anchorMin = Vector2.zero;
        transform.anchorMax = Vector2.one;
        transform.pivot = new Vector2(0.5f, 0.5f);
        transform.anchoredPosition = Vector2.zero;
        transform.sizeDelta = Vector2.zero;
        transform.localScale = Vector3.one;
    }

    public static void MoveLocalCanvas(string goName)
    {
        if (goName == "")
        {
            _drawCanvasForScene!.GetComponent<RectTransform>().SetParent(null, false);
            _drawCanvasForScene.transform.localPosition = Vector3.zero;
        }
        else
        {
            var go = GameObject.Find(goName);
            _drawCanvasForScene!.GetComponent<RectTransform>().SetParent(go.transform, false);
            _drawCanvasForScene.transform.localPosition = Vector3.zero;
        }
    }

    public static Transform GetDrawCanvasForScene()
    {
        return _drawCanvasForScene!.transform;
    }

    public static Transform GetDrawCanvasNoDestroyForScene()
    {
        return _drawCanvasForSceneNoDestroy!.transform;
    }

    public static CanvasGroup GetDrawCanvasForSceneCanvasGroup()
    {
        return _drawCanvasForSceneCanvasGroup!;
    }

    public static CanvasGroup GetDrawCanvasNoDestroyForSceneCanvasGroup()
    {
        return _drawCanvasForSceneNoDestroyCanvasGroup!;
    }

    public static FontTMPManager GetFontManager()
    {
        if (_fontMgr != null) return _fontMgr;
        _fontMgr = GameObject.Find("FontTMPManager")!.GetComponent<FontTMPManager>();
        return _fontMgr!;
    }

    public static ControllerManager GetControllerManager()
    {
        if (_controllerManager != null) return _controllerManager;
        _controllerManager = GameObject.Find("ControllerManager")!.GetComponent<ControllerManager>();
        return _controllerManager!;
    }
}