﻿using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TnTRFMod.Ui;

public class Common
{
    public const int ScreenWidth = 1920;
    public const int ScreenHeight = 1080;
    private static FontTMPManager _fontMgr;
    private static GameObject _drawCanvasForScene;
    private static GameObject _drawCanvasForSceneNoDestroy;
    private static ControllerManager _controllerManager;
    private static bool inited;

    public static void Init()
    {
        if (inited) return;
        inited = true;
        _drawCanvasForSceneNoDestroy = new GameObject("CanvasForTnTRFModNoDestroy");
        Object.DontDestroyOnLoad(_drawCanvasForSceneNoDestroy);
        _drawCanvasForSceneNoDestroy.hideFlags = HideFlags.HideAndDontSave;

        var canvas = _drawCanvasForSceneNoDestroy.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = _drawCanvasForSceneNoDestroy.AddComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(ScreenWidth, ScreenHeight);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        _drawCanvasForSceneNoDestroy.AddComponent<GraphicRaycaster>();
        _drawCanvasForSceneNoDestroy.layer = LayerMask.NameToLayer("UI");
        _drawCanvasForSceneNoDestroy.SetActive(true);
    }

    public static void InitLocal()
    {
        _drawCanvasForScene = new GameObject("CanvasForTnTRFMod");
        var canvas = _drawCanvasForScene.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        var scaler = _drawCanvasForScene.AddComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(ScreenWidth, ScreenHeight);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        _drawCanvasForScene.AddComponent<GraphicRaycaster>();
        _drawCanvasForScene.layer = LayerMask.NameToLayer("UI");
    }

    public static Transform GetDrawCanvasForScene()
    {
        return _drawCanvasForScene!.transform!;
    }

    public static Transform GetDrawCanvasNoDestroyForScene()
    {
        return _drawCanvasForSceneNoDestroy!.transform!;
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