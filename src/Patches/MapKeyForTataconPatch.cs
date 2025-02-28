using System.Runtime.InteropServices;
using HarmonyLib;
using Scripts.OutGame.SongSelect;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TnTRFMod.Patches;

// [HarmonyPatch]
public class MapKeyForTataconPatch
{
    private static UiSongScroller getUiSongScroller()
    {
        var go = GameObject.Find("UiSongScroller");
        if (go == null) return null;
        return go.TryGetComponent<UiSongScroller>(out var scroller) ? scroller : null;
    }

    [HarmonyPatch(typeof(ControllerManager))]
    [HarmonyPatch(nameof(ControllerManager.GetKeyboardKey))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPrefix]
    public static bool ControllerManager_IsKeyDown_Prefix(ref bool __result,
        [In] ref Keyboard keyboard,
        ref Key key)
    {
        if (getUiSongScroller() == null) return true;

        switch (key)
        {
            case Key.W:
            case Key.S:
            case Key.A:
            case Key.D:
                __result = false;
                return false;
        }

        return true;
    }
}