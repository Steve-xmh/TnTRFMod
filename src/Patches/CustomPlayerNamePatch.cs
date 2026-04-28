using HarmonyLib;
using TnTRFMod.Config;
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
        __instance.playerName.SetTextRaw(ModConfig.CustomPlayerName.Value, __instance.playerName.font);
    }
}