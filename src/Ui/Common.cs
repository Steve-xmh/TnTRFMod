using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TnTRFMod.Ui;

public class Common
{
    public const int ScreenWidth = 1920;
    public const int ScreenHeight = 1080;
    private static FontTMPManager _fontMgr;
    private static GameObject _drawCanvasForScene;
    private static ControllerManager _controllerManager;

    public static Transform GetDrawCanvasForScene()
    {
        if (_drawCanvasForScene != null && _drawCanvasForScene.scene == SceneManager.GetActiveScene())
            return _drawCanvasForScene.transform;

        _drawCanvasForScene = GameObject.Find("CanvasForTnTRFMod");
        if (_drawCanvasForScene != null) return _drawCanvasForScene.transform;
        _drawCanvasForScene = new GameObject("CanvasForTnTRFMod");
        var canvas = _drawCanvasForScene.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = _drawCanvasForScene.AddComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(ScreenWidth, ScreenHeight);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        _drawCanvasForScene.AddComponent<GraphicRaycaster>();
        _drawCanvasForScene.layer = LayerMask.NameToLayer("UI");

        return _drawCanvasForScene.transform;
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