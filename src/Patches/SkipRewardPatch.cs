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
    [HarmonyPatch(typeof(ResultPlayer._ShowDonCoinAndRewardAsync_d__164))]
    [HarmonyPatch(nameof(ResultPlayer._ShowDonCoinAndRewardAsync_d__164.MoveNext))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPrefix]
    public static void ResultPlayer__ShowDonCoinAndRewardAsync_d__164_MoveNext_Prefix(
        ResultPlayer._ShowDonCoinAndRewardAsync_d__164 __instance)
    {
        __instance.__1__state = 2;
    }
}