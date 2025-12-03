// The patch is from https://github.com/Deathbloodjr/RF.SkipCoinAndRewardScreen
// Under MIT License

using HarmonyLib;
#if BEPINEX
using Scripts.CrownPoint;

#elif MELONLOADER
using Il2CppScripts.CrownPoint;
#endif

namespace TnTRFMod.Patches;

[HarmonyPatch]
internal class SkipRewardPatch
{
    [HarmonyPatch(typeof(CrownPointManager))]
    [HarmonyPatch(nameof(CrownPointManager.GetCrownPointData))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPostfix]
    public static void CrownPointManager_GetCrownPointData_Postfix(CrownPointManager __instance,
        ref CrownPointData __result)
    {
        __result = new CrownPointData
        {
            CurrentPoints = __result.CurrentPoints,
            SavedPoints = __result.CurrentPoints
        };
    }

    [HarmonyPatch(typeof(ResultCoinExp))]
    [HarmonyPatch(nameof(ResultCoinExp.Start))]
    [HarmonyPostfix]
    public static void ResultCoinExp_Start_Postfix(ResultCoinExp __instance)
    {
        __instance.gameObject.SetActive(false);
        __instance.m_state = ResultCoinExp.State.Show;
        __instance.Hide();
        __instance.Skip();
    }

    [HarmonyPatch(typeof(ResultPlayer._ShowDonCoinAndRewardAsync_d__164))]
    [HarmonyPatch(nameof(ResultPlayer._ShowDonCoinAndRewardAsync_d__164.MoveNext))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPostfix]
    public static void ResultPlayer__ShowDonCoinAndRewardAsync_d__164_MoveNext_Postfix(
        ResultPlayer._ShowDonCoinAndRewardAsync_d__164 __instance)
    {
        __instance.__4__this.flowerMask.enabled = true;
    }
}