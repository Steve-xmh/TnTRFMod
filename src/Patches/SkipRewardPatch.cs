using HarmonyLib;

namespace TnTRFMod.Patches;

[HarmonyPatch]
internal class SkipRewardPatch
{
    [HarmonyPatch(typeof(ResultPlayer))]
    [HarmonyPatch(nameof(ResultPlayer.SettingDonCoinAndReward))]
    [HarmonyPostfix]
    public static void ResultPlayer_SettingDonCoinAndReward_Postfix(ResultPlayer __instance)
    {
        if (!TnTrfMod.Instance.enableSkipRewardPatch.Value) return;
        var levelUnlockCount = __instance.resultCoinExp.PlayerData.BankStockCount();
        var level = __instance.resultCoinExp.PlayerData.CurrentLevel();
        if (levelUnlockCount >= 5 && level >= 400) __instance.isSkipCoinAndReward = true;
    }
}