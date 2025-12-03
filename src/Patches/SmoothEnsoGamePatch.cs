using HarmonyLib;
using UnityEngine;

namespace TnTRFMod.Patches;

[HarmonyPatch]
public class SmoothEnsoGamePatch
{
    private static double SmoothDelta;

    [HarmonyPatch(typeof(EnsoGameManager))]
    [HarmonyPatch(nameof(EnsoGameManager.UpdateSongAdjustParamsNew))]
    [HarmonyPrefix]
    public static bool EnsoGameManager_UpdateSongAdjustParamsNew_Prefix(ref EnsoGameManager __instance)
    {
        var ensoParam = __instance.ensoParam;
        if (ensoParam == null) return false;
        var frameResults = ensoParam.GetFrameResults();
        if (frameResults == null || frameResults.isAllOnpuEnd) return false;
        var ensoSound = __instance.ensoSound;
        if (ensoSound == null || !ensoSound.IsSongPlaying()) return false;

        var songPositionDirect = __instance.songPositionDirect;

        var instantDelta = songPositionDirect -
            (__instance.totalTime - EnsoData.TimeAdjustBaseDelay) * EnsoData.SongTimeScale + __instance.adjustTime;

        var deltaTime = Time.deltaTime;
        const double TotalKeepTime = 10.0;
        var keepTime = TotalKeepTime - deltaTime;

        SmoothDelta = (SmoothDelta * keepTime + instantDelta * deltaTime) / TotalKeepTime;

        __instance.totalTime += deltaTime * SmoothDelta;

        __instance.adjustCounter = 0;
        __instance.adjustSubTime = 0.0;
        __instance.pauseAdjustCounter = 0;

        return false; // 阻止原函数执行
    }
}