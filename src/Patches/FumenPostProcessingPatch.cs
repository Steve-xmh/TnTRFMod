using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TnTRFMod.Ui;
using TnTRFMod.Ui.Widgets;
using TnTRFMod.Utils.Fumen;
using Logger = TnTRFMod.Utils.Logger;
using UnityEngine;

namespace TnTRFMod.Patches;

[HarmonyPatch]
public class FumenPostProcessingPatch
{
    public static bool EnableEqualScrollSpeed = false;
    public static bool EnableSuperSlowScrollSpeed = false;
    public static bool EnableRandomScrollSpeed = false;
    public static bool EnableReverseSlowScrollSpeed = false;
    public static bool EnableStrictJudgeTiming = false;

    public static bool HasAnyPostProcessing => EnableEqualScrollSpeed || EnableSuperSlowScrollSpeed ||
                                               EnableRandomScrollSpeed || EnableReverseSlowScrollSpeed ||
                                               EnableStrictJudgeTiming;

    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(typeof(FumenLoader.PlayerData))]
    [HarmonyPatch(nameof(FumenLoader.PlayerData.WriteFumenBuffer))]
    [HarmonyPrefix]
    private static void FumenLoader_PlayerData_WriteFumenBuffer_Prefix(ref Il2CppStructArray<byte> data)
    {
        var reader = new FumenReader(data);

        Logger.Info($"Fumen HP per good: {reader.HpPerGood}");
        Logger.Info($"Fumen MaxCombo: {reader.MaxCombo}");
        Logger.Info($"Fumen MaxScore: {reader.HpPerGood * reader.MaxCombo}");

        if (EnableEqualScrollSpeed)
            reader.MakeScrollSpeedEqual();
        if (EnableRandomScrollSpeed)
            reader.MakeScrollSpeedRandom();
        if (EnableSuperSlowScrollSpeed)
            reader.MakeScrollSpeedSuperSlow();
        if (EnableReverseSlowScrollSpeed)
            reader.MakeScrollSpeedReverse();
        if (EnableStrictJudgeTiming)
            // reader.ResetJudgeTiming(12.5f, 37.5f, 54f);
            reader.ResetJudgeTiming(10f, 35f, 45f);

        data = reader.fumenData;
    }

    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(typeof(IconOptionEnso))]
    [HarmonyPatch(nameof(IconOptionEnso.Update))]
    [HarmonyPrefix]
    private static void IconOption_SetIcons_Postfix(IconOptionEnso __instance)
    {
        if (__instance.isInitialized) return;
        var initX = -111f;

        if (EnableEqualScrollSpeed)
            IconOption_AddIcon(__instance, TextureManager.Textures.FumenPostEqualScrollSpeed, ref initX);
        if (EnableRandomScrollSpeed)
            IconOption_AddIcon(__instance, TextureManager.Textures.FumenPostRandomScrollSpeed, ref initX);
        if (EnableSuperSlowScrollSpeed)
            IconOption_AddIcon(__instance, TextureManager.Textures.FumenPostSuperSlowScrollSpeed, ref initX);
        if (EnableReverseSlowScrollSpeed)
            IconOption_AddIcon(__instance, TextureManager.Textures.FumenPostReverseSlowScrollSpeed, ref initX);
        if (EnableStrictJudgeTiming)
            IconOption_AddIcon(__instance, TextureManager.Textures.FumenPostStrictJudgeTiming, ref initX);
    }

    private static void IconOption_AddIcon(IconOptionEnso __instance, TextureManager.TexHandle icon, ref float initX)
    {
        const float initY = -35f;

        var iconUi = new ImageUi(icon);
        iconUi.Name = "FumenPostProcessingIcon";
        iconUi._transform.parent = __instance.gameObject.transform;
        iconUi._transform.localPosition = new Vector3(initX, initY, 0f);

        initX += 45f;
    }
}