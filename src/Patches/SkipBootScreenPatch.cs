#if BEPINEX
using Scripts.OutGame.Boot;
using Scripts.OutGame.Common;
#endif

using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
#if MELONLOADER
using Il2CppScripts.OutGame.Boot;
using Il2CppScripts.OutGame.Common;
#endif

namespace TnTRFMod.Patches;

[HarmonyPatch]
internal class SkipBootScreenPatch
{
    private static bool IsBootScene()
    {
        return SceneManager.GetActiveScene().name == "Boot";
    }

    [HarmonyPatch(typeof(BootImage))]
    [HarmonyPatch(nameof(BootImage.PlayAsync))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPrefix]
    private static void BootImage_PlayAsync_Prefix(BootImage __instance, ref float duration, ref bool skippable)
    {
        if (!TnTrfMod.Instance.enableSkipBootScreenPatch.Value) return;
        duration = 0f;
        skippable = true;
    }

    [HarmonyPatch(typeof(FadeCover))]
    [HarmonyPatch(nameof(FadeCover.FadeOutAsync))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPrefix]
    private static void FadeCover_FadeOutAsync_Prefix(FadeCover __instance, ref Color color, ref float duration)
    {
        if (!TnTrfMod.Instance.enableSkipBootScreenPatch.Value) return;
        if (IsBootScene()) duration = 0f;
    }

    [HarmonyPatch(typeof(FadeCover))]
    [HarmonyPatch(nameof(FadeCover.FadeInAsync))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPrefix]
    private static void FadeCover_FadeInAsync_Prefix(FadeCover __instance, ref Color color, ref float duration)
    {
        if (!TnTrfMod.Instance.enableSkipBootScreenPatch.Value) return;
        if (IsBootScene()) duration = 0f;
    }
}