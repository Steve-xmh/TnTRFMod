using HarmonyLib;
#if BEPINEX
using Scripts.Common;
#endif

#if MELONLOADER
using Il2CppScripts.Common;
#endif
namespace TnTRFMod.Patches;

[HarmonyPatch]
internal class CustomPlayerNamePatch
{
    [HarmonyPatch(typeof(UiPlayerNameplate), nameof(UiPlayerNameplate.UpdateNameplate))]
    [HarmonyPostfix]
    private static void UiPlayerNameplate_UpdateNameplate_Prefix(ref UiPlayerNameplate __instance)
    {
        __instance.playerName.SetTextRaw(TnTrfMod.Instance.customPlayerName.Value, __instance.playerName.font);
    }
}