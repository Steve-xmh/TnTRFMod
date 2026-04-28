using UnityEngine.InputSystem;

namespace TnTRFMod.Config;

/// <summary>
/// 全局 Mod 配置项集中管理。
/// 所有 ConfigEntry 和 KeyBindingConfigEntry 均在此定义，方便维护和引用。
/// 使用：<c>ModConfig.EnableMod.Value</c> 替代 <c>TnTrfMod.Instance.enableMod.Value</c>
/// </summary>
public static class ModConfig
{
    // =========================================================================
    // General 节
    // =========================================================================
    public static ConfigEntry<bool> EnableMod { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<bool> EnableBetterBigHitPatch { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<bool> BetterBigHitSkipOnlineCheck { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<bool> EnableSkipBootScreenPatch { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<bool> EnableSkipRewardPatch { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<bool> EnableMinimumLatencyAudioClient { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<bool> EnableAutoDownloadSubscriptionSongs { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<bool> EnableOnpuTextRail { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<bool> EnableHighPrecisionTimerPatch { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<bool> EnableNearestNeighborOnpuPatch { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<bool> EnableNoShadowOnpuPatch { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<bool> EnableCustomDressAnimationMod { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<bool> EnableHitStatsPanelPatch { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<bool> EnableLouderSongPatch { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<bool> EnableScoreRankIcon { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<bool> EnableTatakonKeyboardSongSelect { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<bool> EnableInstantRelayPatch { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<int> ModifyMeasuresCapacity { get; private set; } = ConfigEntry<int>.Noop;
    public static ConfigEntry<string> CustomTitleSceneEnterSceneName { get; private set; } = ConfigEntry<string>.Noop;
    public static ConfigEntry<float> AutoPlayRendaSpeed { get; private set; } = ConfigEntry<float>.Noop;

    // =========================================================================
    // HitOffset 节
    // =========================================================================
    public static ConfigEntry<bool> EnableHitOffset { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<bool> HitOffsetInvertColor { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<float> HitOffsetRyoRange { get; private set; } = ConfigEntry<float>.Noop;

    // =========================================================================
    // BilibiliLiveStreamSongRequest 节
    // =========================================================================
    public static ConfigEntry<bool> EnableBilibiliLiveStreamSongRequest { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<uint> BilibiliLiveStreamSongRoomId { get; private set; } = ConfigEntry<uint>.Noop;
    public static ConfigEntry<string> BilibiliLiveStreamSongToken { get; private set; } = ConfigEntry<string>.Noop;

    // =========================================================================
    // CustomPlayerName 节
    // =========================================================================
    public static ConfigEntry<bool> EnableCustomPlayerName { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<string> CustomPlayerName { get; private set; } = ConfigEntry<string>.Noop;

    // =========================================================================
    // BufferedInput 节
    // =========================================================================
    public static ConfigEntry<bool> EnableBufferedInputPatch { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<int> MaxBufferedInputCount { get; private set; } = ConfigEntry<int>.Noop;

    // =========================================================================
    // TokkunMode 节
    // =========================================================================
    public static ConfigEntry<bool> EnableTokkunGamePatch { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<string> TokkunGameOnSongEndBehaviour { get; private set; } = ConfigEntry<string>.Noop;
    public static ConfigEntry<string> TokkunGameOnPauseBehaviour { get; private set; } = ConfigEntry<string>.Noop;
    public static ConfigEntry<double> TokkunGameSlowTimeOffset { get; private set; } = ConfigEntry<double>.Noop;
    public static ConfigEntry<double> TokkunGameFastTimeOffset { get; private set; } = ConfigEntry<double>.Noop;
    public static KeyBindingConfigEntry P2LeftDonKey { get; private set; } = KeyBindingConfigEntry.Noop;
    public static KeyBindingConfigEntry P2LeftKaKey { get; private set; } = KeyBindingConfigEntry.Noop;
    public static KeyBindingConfigEntry P2RightDonKey { get; private set; } = KeyBindingConfigEntry.Noop;
    public static KeyBindingConfigEntry P2RightKaKey { get; private set; } = KeyBindingConfigEntry.Noop;

    // =========================================================================
    // Debug 节
    // =========================================================================
    public static ConfigEntry<bool> DebugSaveRawSaveData { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<bool> DebugExportGameData { get; private set; } = ConfigEntry<bool>.Noop;
    public static ConfigEntry<bool> DebugExportMusicNames { get; private set; } = ConfigEntry<bool>.Noop;

    /// <summary>
    /// 注册并加载所有配置项。应在 Mod 初始化早期调用。
    /// </summary>
    internal static void Register()
    {
        ConfigSectionBuilder.Section("General", s =>
        {
            // 默认启用的功能
            EnableMod = s.Bool("Enabled", "config.Enabled", true);
            EnableBetterBigHitPatch = s.Bool("EnableBetterBigHitPatch", "config.EnableBetterBigHitPatch", true);
            BetterBigHitSkipOnlineCheck =
                s.Bool("BetterBigHitSkipOnlineCheck", "config.BetterBigHitSkipOnlineCheck", false);
            EnableSkipBootScreenPatch = s.Bool("EnableSkipBootScreenPatch", "config.EnableSkipBootScreenPatch", true);
            EnableSkipRewardPatch = s.Bool("EnableSkipRewardPatch", "config.EnableSkipRewardPatch", true);
            EnableMinimumLatencyAudioClient = s.Bool("EnableMinimumLatencyAudioClient",
                "config.EnableMinimumLatencyAudioClient", true);
            EnableAutoDownloadSubscriptionSongs = s.Bool("EnableAutoDownloadSubscriptionSongs",
                "config.EnableAutoDownloadSubscriptionSongs", true);
            EnableOnpuTextRail = s.Bool("EnableOnpuTextRail", "config.EnableOnpuTextRail", true);
            EnableHighPrecisionTimerPatch =
                s.Bool("EnableHighPrecisionTimerPatch", "config.EnableHighPrecisionTimerPatch", true);
            // 默认禁用的功能
            EnableNearestNeighborOnpuPatch = s.Bool("EnableNearestNeighborOnpuPatch",
                "config.EnableNearestNeighborOnpuPatch", false);
            EnableNoShadowOnpuPatch = s.Bool("EnableNoShadowOnpuPatch", "config.EnableNoShadowOnpuPatch", false);
            EnableCustomDressAnimationMod = s.Bool("EnableCustomDressAnimationMod",
                "config.EnableCustomDressAnimationMod", false);
            EnableHitStatsPanelPatch = s.Bool("EnableHitStatsPanelPatch", "config.EnableHitStatsPanelPatch", false);
            EnableLouderSongPatch = s.Bool("EnableLouderSongPatch", "config.EnableLouderSongPatch", false);
            EnableScoreRankIcon = s.Bool("EnableScoreRankIcon", "config.EnableScoreRankIcon", false);
            EnableTatakonKeyboardSongSelect = s.Bool("EnableTatakonKeyboardSongSelect",
                "config.EnableTatakonKeyboardSongSelect", false);
            EnableInstantRelayPatch = s.Bool("EnableInstantRelayPatch", "config.EnableInstantRelayPatch", true);
            // 数值型配置
            ModifyMeasuresCapacity = s.Int("ModifyMeasuresCapacity", "config.ModifyMeasuresCapacity", 65536);
            CustomTitleSceneEnterSceneName = s.String("CustomTitleSceneEnterSceneName",
                "config.CustomTitleSceneEnterSceneName", "");
            AutoPlayRendaSpeed = s.Float("AutoPlayRendaSpeed", "config.AutoPlayRendaSpeed", 30f);
        });

        ConfigSectionBuilder.Section("HitOffset", s =>
        {
            EnableHitOffset = s.Bool("Enable", "config.HitOffset.Enable", false);
            HitOffsetInvertColor = s.Bool("InvertColor", "config.HitOffset.InvertColor", false);
            HitOffsetRyoRange = s.Float("RyoRange", "config.HitOffset.RyoRange", -1f);
        });

        ConfigSectionBuilder.Section("BilibiliLiveStreamSongRequest", s =>
        {
            EnableBilibiliLiveStreamSongRequest =
                s.Bool("Enable", "config.BilibiliLiveStreamSongRequest.Enable", false);
            BilibiliLiveStreamSongRoomId = s.UInt("RoomId", "config.BilibiliLiveStreamSongRequest.RoomId", 0u);
            BilibiliLiveStreamSongToken = s.String("Token", "config.BilibiliLiveStreamSongRequest.Token", "");
        });

        ConfigSectionBuilder.Section("CustomPlayerName", s =>
        {
            EnableCustomPlayerName = s.Bool("Enable", "config.CustomPlayerName.Enable", false);
            CustomPlayerName = s.String("Name", "config.CustomPlayerName.Name", "Don-chan");
        });

        ConfigSectionBuilder.Section("BufferedInput", s =>
        {
            EnableBufferedInputPatch = s.Bool("Enable", "config.BufferedInput.Enable", true);
            MaxBufferedInputCount = s.Int("MaxBufferedInputCount", "config.BufferedInput.MaxBufferedInputCount", 5);
        });

        ConfigSectionBuilder.Section("TokkunMode", s =>
        {
            EnableTokkunGamePatch = s.Bool("Enable", "config.TokkunMode.Enable", false);
            TokkunGameOnSongEndBehaviour =
                s.String("OnSongEndBehaviour", "config.TokkunMode.OnSongEndBehaviour", "ToSongStart");
            TokkunGameOnPauseBehaviour = s.String("OnPauseBehaviour", "config.TokkunMode.OnPauseBehaviour",
                "PauseAtCurrentPosition");
            TokkunGameSlowTimeOffset = s.Double("SlowTimeOffset", "config.TokkunMode.SlowTimeOffset", -100.0);
            TokkunGameFastTimeOffset = s.Double("FastTimeOffset", "config.TokkunMode.FastTimeOffset", 0.0);
            // 按键映射
            P2LeftDonKey = s.KeyBinding("P2LeftDonKey", "config.TokkunMode.P2LeftDonKey", Key.X);
            P2LeftKaKey = s.KeyBinding("P2LeftKaKey", "config.TokkunMode.P2LeftKaKey", Key.Z);
            P2RightDonKey = s.KeyBinding("P2RightDonKey", "config.TokkunMode.P2RightDonKey", Key.C);
            P2RightKaKey = s.KeyBinding("P2RightKaKey", "config.TokkunMode.P2RightKaKey", Key.V);
        });

        ConfigSectionBuilder.Section("Debug", s =>
        {
            DebugSaveRawSaveData = s.Bool("SaveRawSaveData", "config.Debug.SaveRawSaveData", false);
            DebugExportGameData = s.Bool("ExportGameData", "config.Debug.ExportGameData", false);
            DebugExportMusicNames = s.Bool("ExportMusicNames", "config.Debug.ExportMusicNames", false);
        });

        ConfigEntry.Load();
        KeyBindingConfigEntry.Load();
    }
}