using Il2Cpp;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TnTRFMod.Ui;

public class Common
{
    public const int ScreenWidth = 1920;
    public const int ScreenHeight = 1080;
    private static FontTMPManager _fontMgr;
    private static GameObject _drawCanvas;

    public static Transform GetDrawCanvas()
    {
        if (_drawCanvas != null && _drawCanvas.scene == SceneManager.GetActiveScene())
            return _drawCanvas.transform;

        _drawCanvas = GameObject.Find("CanvasForTnTRFMod");
        if (_drawCanvas != null) return _drawCanvas.transform;
        _drawCanvas = new GameObject("CanvasForTnTRFMod");
        var canvas = _drawCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = _drawCanvas.AddComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(ScreenWidth, ScreenHeight);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        _drawCanvas.AddComponent<GraphicRaycaster>();
        _drawCanvas.layer = LayerMask.NameToLayer("UI");
        return _drawCanvas.transform;
    }

    public static FontTMPManager GetFontManager()
    {
        if (_fontMgr != null) return _fontMgr;
        _fontMgr = GameObject.Find("FontTMPManager")!.GetComponent<FontTMPManager>();
        return _fontMgr!;
    }
}