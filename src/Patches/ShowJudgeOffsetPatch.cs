using HarmonyLib;
using UnityEngine;
using Logger = TnTRFMod.Utils.Logger;

namespace TnTRFMod.Patches;

[HarmonyPatch]
public class ShowJudgeOffsetPatch
{
    public static float LastHitTimeOffset;
    public static int RyoCount;
    public static int KaCount;
    public static int FuKaCount;
    public static int RendaCount;

    public static float RyoJudgeRange = float.Epsilon;
    public static float KaJudgeRange = float.Epsilon;
    public static float FukaJudgeRange = float.Epsilon;

    [HarmonyPatch(typeof(EnsoGameManager))]
    [HarmonyPatch(nameof(EnsoGameManager.ProcLoading))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPostfix]
    private static void EnsoGameManager_ProcLoading_Postfix(EnsoGameManager __instance)
    {
        BufferedNoteInputPatch.ResetCounts();
        RyoCount = 0;
        KaCount = 0;
        FuKaCount = 0;
        RendaCount = 0;
        LastHitTimeOffset = 0;

        // var results = __instance.ensoParam.GetFrameResults();
        // var i = 0;
        // foreach (var result in results.eachPlayer)
        // {
        //     var ryoRange = result.GetJudgeRange(TaikoCoreTypes.OnpuTypes.Don, TaikoCoreTypes.HitResultTypes.Ryo);
        //     var kaRange = result.GetJudgeRange(TaikoCoreTypes.OnpuTypes.Don, TaikoCoreTypes.HitResultTypes.Ka);
        //     var fukaRange = result.GetJudgeRange(TaikoCoreTypes.OnpuTypes.Don, TaikoCoreTypes.HitResultTypes.Fuka);
        //     // Logger.Info(
        //     //     $"Player {++i} ryoRange {ryoRange}ms kaRange {kaRange}ms fukaRange {fukaRange}ms hitResultInfoMax {results.hitResultInfoMax} hitResultInfoNum {results.hitResultInfoNum}");
        // }
    }

    // EnsoGameManager__ProcExecMain
    [HarmonyPatch(typeof(EnsoGameManager))]
    [HarmonyPatch(nameof(EnsoGameManager.ProcExecMain))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPostfix]
    private static void EnsoGameManager_ProcExecMain_Postfix(EnsoGameManager __instance)
    {
        var results = __instance.ensoParam.GetFrameResults();

        try
        {
            var eachPlayer = results.eachPlayer[0];
            RyoJudgeRange = eachPlayer.GetJudgeRange(TaikoCoreTypes.OnpuTypes.Don, TaikoCoreTypes.HitResultTypes.Ryo);
            KaJudgeRange = eachPlayer.GetJudgeRange(TaikoCoreTypes.OnpuTypes.Don, TaikoCoreTypes.HitResultTypes.Ka);
            FukaJudgeRange = eachPlayer.GetJudgeRange(TaikoCoreTypes.OnpuTypes.Don, TaikoCoreTypes.HitResultTypes.Fuka);
        }
        catch (Exception e)
        {
            Logger.Warn("Failed to get judge range, fallback to hard/oni judge range");
            RyoJudgeRange = 25.25002f;
            KaJudgeRange = 75.075005f;
            FukaJudgeRange = 108.441666f;
        }

        // Logger.Info($"donRange {ryoRange}ms kaRange {kaRange}ms fukaRange {fukaRange}ms hitResultInfoMax {results.hitResultInfoMax} hitResultInfoNum {results.hitResultInfoNum}");
        // Logger.Info($"results.firstOnpu.state {results.firstOnpu.state}");
        foreach (var hit in results.hitResultInfo.Take(results.hitResultInfoNum))
        {
            var hitResult = (TaikoCoreTypes.HitResultTypes)hit.hitResult;
            var onpuType = (TaikoCoreTypes.OnpuTypes)hit.onpuType;
            if (hitResult == TaikoCoreTypes.HitResultTypes.None) continue;
            // Logger.Info($"- hit.onpu.justTime {hit.onpu.justTime} ({__instance.totalTime - hit.onpu.justTime})");
            // Logger.Info($"  onpuType {onpuType}");
            // Logger.Info($"  hitResult {hitResult}");
            switch (onpuType)
            {
                case TaikoCoreTypes.OnpuTypes.Don:
                case TaikoCoreTypes.OnpuTypes.Do:
                case TaikoCoreTypes.OnpuTypes.Ko:
                case TaikoCoreTypes.OnpuTypes.Katsu:
                case TaikoCoreTypes.OnpuTypes.Ka:
                case TaikoCoreTypes.OnpuTypes.WDon:
                case TaikoCoreTypes.OnpuTypes.DaiDon:
                case TaikoCoreTypes.OnpuTypes.DaiKatsu:
                    switch (hitResult)
                    {
                        case TaikoCoreTypes.HitResultTypes.Ryo:
                            RyoCount++;
                            LastHitTimeOffset = hit.onpu.justTime - (float)__instance.totalTime;
                            break;
                        case TaikoCoreTypes.HitResultTypes.Ka:
                            KaCount++;
                            LastHitTimeOffset = hit.onpu.justTime - (float)__instance.totalTime;
                            break;
                        case TaikoCoreTypes.HitResultTypes.Drop:
                            FuKaCount++;
                            break;
                        case TaikoCoreTypes.HitResultTypes.Fuka:
                            FuKaCount++;
                            LastHitTimeOffset = hit.onpu.justTime - (float)__instance.totalTime;
                            break;
                    }

                    break;
                case TaikoCoreTypes.OnpuTypes.GekiRenda:
                case TaikoCoreTypes.OnpuTypes.DaiRenda:
                case TaikoCoreTypes.OnpuTypes.Renda:
                {
                    switch (hitResult)
                    {
                        case TaikoCoreTypes.HitResultTypes.Ryo:
                        case TaikoCoreTypes.HitResultTypes.Ka:
                            RendaCount++;
                            break;
                    }

                    break;
                }
            }
        }
    }

    // EnsoGameManager__ProcExecMain
    [HarmonyPatch(typeof(EnsoGameManager))]
    [HarmonyPatch(nameof(EnsoGameManager.Update))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPostfix]
    private static void EnsoGameManager_Update_Postfix(EnsoGameManager __instance)
    {
        if (__instance.state == EnsoGameManager.State.ToResult) GameObject.Find("TrainCounterSprite")?.SetActive(false);
    }
}