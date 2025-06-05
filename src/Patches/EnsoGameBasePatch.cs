using HarmonyLib;
using UnityEngine;
using Logger = TnTRFMod.Utils.Logger;

namespace TnTRFMod.Patches;

[HarmonyPatch]
public class EnsoGameBasePatch
{
    public static float LastHitTimeOffset;
    public static float AverageHitTimeOffset;
    private static int HitCount;
    public static int RyoCount;
    public static int KaCount;
    public static int FuKaCount;
    public static int RendaCount;

    public static float RyoJudgeRange = float.Epsilon;
    public static float KaJudgeRange = float.Epsilon;
    public static float FukaJudgeRange = float.Epsilon;

    private static readonly float[] _rendaTimers = new float[5];

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
        HitCount = 0;
        LastHitTimeOffset = 0;
        AverageHitTimeOffset = 0;
    }

    [HarmonyPatch(typeof(EnsoInput))]
    [HarmonyPatch(nameof(EnsoInput.CheckAutoRenda))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPrefix]
    private static bool EnsoInput_CheckAutoRenda_Prefix(EnsoInput __instance, ref bool __result, int player,
        int rendaFrame)
    {
        var speed = TnTrfMod.Instance.autoPlayRendaSpeed.Value;
        if (speed == 0f)
        {
            __result = false;
            return false;
        }

        var playerInfo = __instance.playerInfo[player];
        _rendaTimers[player] = Math.Max(0, _rendaTimers[player] - Time.deltaTime);

        __result = _rendaTimers[player] <= 0;
        if (!__result) return false;
        playerInfo.autoRendaCount++;
        _rendaTimers[player] = speed / 1000f;

        return false;
    }


    private static void OnSimpleHit(TaikoCoreTypes.HitResultTypes hitResult, float onpuJustTime)
    {
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (hitResult)
        {
            case TaikoCoreTypes.HitResultTypes.Ryo:
                RyoCount++;
                HitCount++;
                LastHitTimeOffset = onpuJustTime;
                AverageHitTimeOffset = (onpuJustTime + AverageHitTimeOffset * (HitCount - 1)) / HitCount;
                break;
            case TaikoCoreTypes.HitResultTypes.Ka:
                KaCount++;
                HitCount++;
                LastHitTimeOffset = onpuJustTime;
                AverageHitTimeOffset = (onpuJustTime + AverageHitTimeOffset * (HitCount - 1)) / HitCount;
                break;
            case TaikoCoreTypes.HitResultTypes.Drop:
                FuKaCount++;
                break;
            case TaikoCoreTypes.HitResultTypes.Fuka:
                FuKaCount++;
                LastHitTimeOffset = onpuJustTime;
                break;
        }
    }

    private static void OnRendaHit()
    {
        RendaCount++;
    }

    // EnsoGameManager__ProcExecMain
    // 此函数为逐帧调用，尽量避免产生过多开销
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
        catch (Exception)
        {
            Logger.Warn("Failed to get judge range, fallback to hard/oni judge range");
            RyoJudgeRange = 25.25002f;
            KaJudgeRange = 75.075005f;
            FukaJudgeRange = 108.441666f;
        }

        // Logger.Info($"donRange {ryoRange}ms kaRange {kaRange}ms fukaRange {fukaRange}ms hitResultInfoMax {results.hitResultInfoMax} hitResultInfoNum {results.hitResultInfoNum}");
        // Logger.Info($"results.firstOnpu.state {results.firstOnpu.state}");
        for (var i = 0; i < results.hitResultInfoNum; i++)
        {
            var hit = results.hitResultInfo[i];
            var hitResult = (TaikoCoreTypes.HitResultTypes)hit.hitResult;
            var onpuType = (TaikoCoreTypes.OnpuTypes)hit.onpuType;
            if (hitResult == TaikoCoreTypes.HitResultTypes.None) continue;
            // Logger.Info($"- hit.onpu.justTime {hit.onpu.justTime} ({__instance.totalTime - hit.onpu.justTime})");
            // Logger.Info($"  onpuType {onpuType}");
            // Logger.Info($"  hitResult {hitResult}");
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
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
                    // 音符判定调整： __instance.settings.noteDelay
                    // 太鼓控制器判定调整： __instance.settings.tatakonDelay
                    var onpuJustTime = hit.onpu.justTime - (float)__instance.totalTime -
                                       __instance.settings.noteDelay * 5;
                    // Console.Out.WriteLine($"Onpu Type: {hitResult} noteDelay: {__instance.settings.noteDelay} tatakonDelay: {__instance.settings.tatakonDelay}");
                    OnSimpleHit(hitResult, onpuJustTime);
                    break;
                case TaikoCoreTypes.OnpuTypes.GekiRenda:
                case TaikoCoreTypes.OnpuTypes.DaiRenda:
                case TaikoCoreTypes.OnpuTypes.Renda:
                {
                    switch (hitResult)
                    {
                        case TaikoCoreTypes.HitResultTypes.Ryo:
                        case TaikoCoreTypes.HitResultTypes.Ka:
                            OnRendaHit();
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
    }

    public class HitInfo : EventArgs
    {
        public TaikoCoreTypes.HitResultTypes HitResult;
        public float OnpuJustTime;
    }
}