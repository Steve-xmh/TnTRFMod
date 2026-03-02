using HarmonyLib;
using TnTRFMod.Utils;
#if BEPINEX
using Scripts.Common.LoadingScreen;
using Scripts.Common.Sound;
using Scripts.OutGame.SongSelect;

#elif MELONLOADER
using Il2CppScripts.Common.LoadingScreen;
using Il2CppScripts.Common.Sound;
using Il2CppScripts.OutGame.SongSelect;
#endif

namespace TnTRFMod.Patches;

[HarmonyPatch]
public class InstantRelayPatch
{
    // [HarmonyPatch(MethodType.Normal)]
    // [HarmonyPatch(typeof(LoadingScreen))]
    // [HarmonyPatch(nameof(LoadingScreen.OpenAsync))]
    // [HarmonyPrefix]
    // private static void LoadingScreen_OpenAsync_Prefix(ref bool immediate)
    // {
    //     immediate = true;
    // }
    //
    // [HarmonyPatch(MethodType.Normal)]
    // [HarmonyPatch(typeof(LoadingScreen))]
    // [HarmonyPatch(nameof(LoadingScreen.CloseAsync))]
    // [HarmonyPrefix]
    // private static bool LoadingScreen_CloseAsync_Prefix(ref LoadingScreen __instance, ref UniTask __result)
    // {
    //     __instance.SetOpened(false);
    //     __instance.AnimationType = LoadingScreen.AnimationTypes.Immediate;
    //     __instance.IsAnimating = false;
    //     __result = UniTask.CompletedTask;
    //     return false;
    // }

    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(typeof(LoadingScreen))]
    [HarmonyPatch(nameof(LoadingScreen.OpenAsync))]
    [HarmonyPrefix]
    private static void LoadingScreen_OpenAsync_Prefix(ref LoadingScreen __instance,
        ref LoadingScreen.BackgroundTypes background,
        ref LoadingScreen.TextTypes text,
        ref bool immediate)
    {
        immediate = true;
    }

    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(typeof(SoundManager))]
    [HarmonyPatch(nameof(SoundManager.IsVoicePlaying))]
    [HarmonyPrefix]
    private static bool SoundManager_IsVoicePlaying_Prefix(ref SoundManager __instance,
        ref bool __result)
    {
        __result = false;
        return false;
    }

    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(typeof(SoundManager))]
    [HarmonyPatch(nameof(SoundManager.IsPlayingCommonSe))]
    [HarmonyPrefix]
    private static bool SoundManager_IsPlayingCommonSe_Prefix(ref SoundManager __instance,
        ref bool __result)
    {
        __result = false;
        return false;
    }

    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(typeof(UiSongButtonDifficulty))]
    [HarmonyPatch(nameof(UiSongButtonDifficulty.SetupBand))]
    [HarmonyPrefix]
    private static bool LoadingScreen_CloseAsync_Prefix(ref UiSongButtonDifficulty __instance,
        ref MusicDataInterface.MusicInfoAccesser item, ref NeiroDataInterface.NeiroInfoAccesser neiroData,
        ref int partIndex)
    {
        Logger.Info($"item.stars: {item.Stars.Length}");
        if (item.Stars.Length < 10)
        {
            Logger.Warn("Not enough stars for band difficulty, skip to avoid crashing");
            return false;
        }

        return true;
    }

    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(typeof(UiLoadingScreen))]
    [HarmonyPatch(nameof(UiLoadingScreen.Setup))]
    [HarmonyPostfix]
    private static void UiLoadingScreen_Setup_Postfix(ref UiLoadingScreen __instance)
    {
        __instance.fadeDuration = 0.1f;
    }
}