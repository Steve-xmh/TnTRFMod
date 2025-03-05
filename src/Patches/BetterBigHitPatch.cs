using HarmonyLib;
#if BEPINEX
using Scripts.EnsoGame.Network;
#endif

#if MELONLOADER
using Il2CppScripts.EnsoGame.Network;
#endif

namespace TnTRFMod.Patches;

[HarmonyPatch]
internal class BetterBigHitPatch
{
    [HarmonyPatch(typeof(EnsoInput))]
    [HarmonyPatch(nameof(EnsoInput.GetLastInputForCore))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPostfix]
    private static void EnsoInput_GetLastInputForCore_Postfix(EnsoInput __instance,
        ref TaikoCoreTypes.UserInputType __result,
        int player)
    {
        if (!TnTrfMod.Instance.enableBetterBigHitPatch.Value) return;
        // 在线模式下不对输入进行修改
        if (__instance.ensoParam.networkGameMode != NetworkGameMode.None) return;
        switch (__result)
        {
            case TaikoCoreTypes.UserInputType.Don_Weak:
            case TaikoCoreTypes.UserInputType.Don_Pad:
                __result = TaikoCoreTypes.UserInputType.Don_Strong;
                break;
            case TaikoCoreTypes.UserInputType.Katsu_Weak:
            case TaikoCoreTypes.UserInputType.Katsu_Pad:
                __result = TaikoCoreTypes.UserInputType.Katsu_Strong;
                break;
        }
    }
}