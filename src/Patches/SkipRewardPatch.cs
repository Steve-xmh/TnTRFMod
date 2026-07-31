// The patch is from https://github.com/Deathbloodjr/RF.SkipCoinAndRewardScreen
// Under MIT License


using HarmonyLib;

#if BEPINEX
using Cysharp.Threading.Tasks;

#elif MELONLOADER
using Il2CppCysharp.Threading.Tasks;
#endif

namespace TnTRFMod.Patches;

[HarmonyPatch]
internal class SkipRewardPatch
{
    [HarmonyPatch(typeof(ResultCoinExp))]
    [HarmonyPatch(nameof(ResultCoinExp.Activate))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPrefix]
    private static bool ResultCoinExp_Activate_Prefix(ResultCoinExp __instance, ref UniTask __result)
    {
        __instance.m_state = ResultCoinExp.State.Done;
        __instance.Hide();

        __result = UniTask.CompletedTask;
        return false;
    }
}