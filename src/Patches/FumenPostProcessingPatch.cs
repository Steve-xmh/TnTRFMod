using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TnTRFMod.Utils.Fumen;

namespace TnTRFMod.Patches;

[HarmonyPatch]
public class FumenPostProcessingPatch
{
    public static bool EnableEqualScrollSpeed = false;
    public static bool EnableSuperSlowSpeed = false;
    public static bool EnableRandomSlowSpeed = false;
    public static bool EnableReverseSlowSpeed = false;
    public static bool EnableStrictJudgeTiming = false;

    public static bool HasAnyPostProcessing => EnableEqualScrollSpeed || EnableSuperSlowSpeed ||
                                               EnableRandomSlowSpeed || EnableReverseSlowSpeed ||
                                               EnableStrictJudgeTiming;

    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(typeof(FumenLoader.PlayerData))]
    [HarmonyPatch(nameof(FumenLoader.PlayerData.WriteFumenBuffer))]
    [HarmonyPrefix]
    private static void FumenLoader_PlayerData_WriteFumenBuffer_Prefix(ref Il2CppStructArray<byte> data)
    {
        var reader = new FumenReader(data);

        if (EnableEqualScrollSpeed)
            reader.MakeScrollSpeedEqual();
        if (EnableRandomSlowSpeed)
            reader.MakeScrollSpeedRandom();
        if (EnableSuperSlowSpeed)
            reader.MakeScrollSpeedSuperSlow();
        if (EnableReverseSlowSpeed)
            reader.MakeScrollSpeedReverse();
        if (EnableStrictJudgeTiming)
            // reader.ResetJudgeTiming(12.5f, 37.5f, 54f);
            reader.ResetJudgeTiming(10f, 35f, 45f);

        data = reader.fumenData;
    }
}