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

        var results = __instance.ensoParam.GetFrameResults();
        var i = 0;
        foreach (var result in results.eachPlayer)
        {
            var ryoRange = result.GetJudgeRange(TaikoCoreTypes.OnpuTypes.Don, TaikoCoreTypes.HitResultTypes.Ryo);
            var kaRange = result.GetJudgeRange(TaikoCoreTypes.OnpuTypes.Don, TaikoCoreTypes.HitResultTypes.Ka);
            var fukaRange = result.GetJudgeRange(TaikoCoreTypes.OnpuTypes.Don, TaikoCoreTypes.HitResultTypes.Fuka);
            Logger.Info(
                $"Player {++i} ryoRange {ryoRange}ms kaRange {kaRange}ms fukaRange {fukaRange}ms hitResultInfoMax {results.hitResultInfoMax} hitResultInfoNum {results.hitResultInfoNum}");
        }
    }

    // EnsoGameManager__ProcExecMain
    [HarmonyPatch(typeof(EnsoGameManager))]
    [HarmonyPatch(nameof(EnsoGameManager.ProcExecMain))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPostfix]
    private static void EnsoGameManager_ProcExecMain_Postfix(EnsoGameManager __instance)
    {
        var results = __instance.ensoParam.GetFrameResults();
        foreach (var result in results.eachPlayer)
            // var ryoRange = result.GetJudgeRange(TaikoCoreTypes.OnpuTypes.Don, TaikoCoreTypes.HitResultTypes.Ryo);
            // var kaRange = result.GetJudgeRange(TaikoCoreTypes.OnpuTypes.Don, TaikoCoreTypes.HitResultTypes.Ka);
            // var fukaRange = result.GetJudgeRange(TaikoCoreTypes.OnpuTypes.Don, TaikoCoreTypes.HitResultTypes.Fuka);
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
                            LastHitTimeOffset = (float)__instance.totalTime - hit.onpu.justTime;
                            break;
                        case TaikoCoreTypes.HitResultTypes.Ka:
                            KaCount++;
                            LastHitTimeOffset = (float)__instance.totalTime - hit.onpu.justTime;
                            break;
                        case TaikoCoreTypes.HitResultTypes.Drop:
                            LastHitTimeOffset = 0;
                            FuKaCount++;
                            break;
                        case TaikoCoreTypes.HitResultTypes.Fuka:
                            FuKaCount++;
                            LastHitTimeOffset = (float)__instance.totalTime - hit.onpu.justTime;
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