using UnityEngine.InputSystem;

namespace TnTRFMod.Config;

/// <summary>
/// 配置项类型枚举，用于UI渲染
/// </summary>
public enum ConfigItemType
{
    Bool,
    Int,
    Float,
    Double,
    String,
    UInt,
    KeyBinding
}

/// <summary>
/// 配置项元数据，用于UI枚举和渲染
/// </summary>
public class ConfigItemOption
{
    public object Value { get; init; } = default!;
    public string LabelKey { get; init; } = "";
}

public class ConfigItemMetadata
{
    public string Section { get; init; } = "";
    public string KeyName { get; init; } = "";
    public string DescriptionKey { get; init; } = "";
    public ConfigItemType Type { get; init; }

    /// <summary>此配置项相关的场景列表（用于"当前场景功能"分页过滤），空数组表示全局/不限场景</summary>
    public string[] RelevantScenes { get; init; } = [];

    public Func<object> GetValue { get; init; } = () => default!;
    public Action<object> SetValue { get; init; } = _ => { };

    /// <summary>非空时使用下拉选择器编辑，而不是自由文本输入。</summary>
    public ConfigItemOption[] Options { get; set; } = [];

    /// <summary>是否可在游戏运行期间立即应用，无需重新启动。</summary>
    public bool HotReloadable { get; set; }

    public string CategoryKey => $"{Section}.{KeyName}";
}

/// <summary>
/// 全局 Mod 配置项集中管理。
/// 所有 ConfigEntry 和 KeyBindingConfigEntry 均在此定义，方便维护和引用。
/// 使用：<c>ModConfig.EnableMod.Value</c> 替代 <c>TnTrfMod.Instance.enableMod.Value</c>
/// </summary>
public static class ModConfig
{
    /// <summary>所有配置项的扁平化元数据列表，供设置UI枚举使用</summary>
    public static List<ConfigItemMetadata> AllItems { get; } = [];

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

        // 构建所有配置项的扁平化元数据，供设置UI使用
        AllItems.Clear();

        // General
        AddBool("General", "Enabled", Description("config.Enabled"), EnableMod);
        AddBool("General", "EnableBetterBigHitPatch", Description("config.EnableBetterBigHitPatch"),
            EnableBetterBigHitPatch, "Enso", "EnsoTest", "EnsoNetwork");
        AddBool("General", "BetterBigHitSkipOnlineCheck", Description("config.BetterBigHitSkipOnlineCheck"),
            BetterBigHitSkipOnlineCheck, "Enso", "EnsoTest", "EnsoNetwork");
        AddBool("General", "EnableSkipBootScreenPatch", Description("config.EnableSkipBootScreenPatch"),
            EnableSkipBootScreenPatch, "Boot");
        AddBool("General", "EnableSkipRewardPatch", Description("config.EnableSkipRewardPatch"), EnableSkipRewardPatch);
        AddBool("General", "EnableMinimumLatencyAudioClient", Description("config.EnableMinimumLatencyAudioClient"),
            EnableMinimumLatencyAudioClient);
        AddBool("General", "EnableAutoDownloadSubscriptionSongs",
            Description("config.EnableAutoDownloadSubscriptionSongs"), EnableAutoDownloadSubscriptionSongs,
            "SongSelect");
        AddBool("General", "EnableOnpuTextRail", Description("config.EnableOnpuTextRail"), EnableOnpuTextRail, "Enso",
            "EnsoTest");
        AddBool("General", "EnableHighPrecisionTimerPatch", Description("config.EnableHighPrecisionTimerPatch"),
            EnableHighPrecisionTimerPatch);
        AddBool("General", "EnableNearestNeighborOnpuPatch", Description("config.EnableNearestNeighborOnpuPatch"),
            EnableNearestNeighborOnpuPatch, "Enso", "EnsoTest", "EnsoNetwork");
        AddBool("General", "EnableNoShadowOnpuPatch", Description("config.EnableNoShadowOnpuPatch"),
            EnableNoShadowOnpuPatch, "Enso", "EnsoTest", "EnsoNetwork");
        AddBool("General", "EnableCustomDressAnimationMod", Description("config.EnableCustomDressAnimationMod"),
            EnableCustomDressAnimationMod, "DressUp");
        AddBool("General", "EnableHitStatsPanelPatch", Description("config.EnableHitStatsPanelPatch"),
            EnableHitStatsPanelPatch, "Enso", "EnsoTest");
        AddBool("General", "EnableLouderSongPatch", Description("config.EnableLouderSongPatch"), EnableLouderSongPatch);
        AddBool("General", "EnableScoreRankIcon", Description("config.EnableScoreRankIcon"), EnableScoreRankIcon,
            "Enso", "EnsoTest");
        AddBool("General", "EnableTatakonKeyboardSongSelect", Description("config.EnableTatakonKeyboardSongSelect"),
            EnableTatakonKeyboardSongSelect, "SongSelect");
        AddBool("General", "EnableInstantRelayPatch", Description("config.EnableInstantRelayPatch"),
            EnableInstantRelayPatch);
        AddInt("General", "ModifyMeasuresCapacity", Description("config.ModifyMeasuresCapacity"),
            ModifyMeasuresCapacity);
        AddString("General", "CustomTitleSceneEnterSceneName", Description("config.CustomTitleSceneEnterSceneName"),
            CustomTitleSceneEnterSceneName);
        AddFloat("General", "AutoPlayRendaSpeed", Description("config.AutoPlayRendaSpeed"), AutoPlayRendaSpeed, "Enso",
            "EnsoTest");

        // HitOffset
        AddBool("HitOffset", "Enable", Description("config.HitOffset.Enable"), EnableHitOffset, "Enso", "EnsoTest");
        AddBool("HitOffset", "InvertColor", Description("config.HitOffset.InvertColor"), HitOffsetInvertColor, "Enso",
            "EnsoTest");
        AddFloat("HitOffset", "RyoRange", Description("config.HitOffset.RyoRange"), HitOffsetRyoRange, "Enso",
            "EnsoTest");

        // BilibiliLiveStreamSongRequest
        AddBool("BilibiliLiveStreamSongRequest", "Enable", Description("config.BilibiliLiveStreamSongRequest.Enable"),
            EnableBilibiliLiveStreamSongRequest, "Boot", "SongSelect", "Enso");
        AddUInt("BilibiliLiveStreamSongRequest", "RoomId", Description("config.BilibiliLiveStreamSongRequest.RoomId"),
            BilibiliLiveStreamSongRoomId, "Boot", "SongSelect");
        AddString("BilibiliLiveStreamSongRequest", "Token", Description("config.BilibiliLiveStreamSongRequest.Token"),
            BilibiliLiveStreamSongToken, "Boot", "SongSelect");

        // CustomPlayerName
        AddBool("CustomPlayerName", "Enable", Description("config.CustomPlayerName.Enable"), EnableCustomPlayerName);
        AddString("CustomPlayerName", "Name", Description("config.CustomPlayerName.Name"), CustomPlayerName);

        // BufferedInput
        AddBool("BufferedInput", "Enable", Description("config.BufferedInput.Enable"), EnableBufferedInputPatch);
        AddInt("BufferedInput", "MaxBufferedInputCount", Description("config.BufferedInput.MaxBufferedInputCount"),
            MaxBufferedInputCount);

        // TokkunMode
        AddBool("TokkunMode", "Enable", Description("config.TokkunMode.Enable"), EnableTokkunGamePatch, "Enso");
        AddString("TokkunMode", "OnSongEndBehaviour", Description("config.TokkunMode.OnSongEndBehaviour"),
            TokkunGameOnSongEndBehaviour, "Enso");
        AddString("TokkunMode", "OnPauseBehaviour", Description("config.TokkunMode.OnPauseBehaviour"),
            TokkunGameOnPauseBehaviour, "Enso");
        AddDouble("TokkunMode", "SlowTimeOffset", Description("config.TokkunMode.SlowTimeOffset"),
            TokkunGameSlowTimeOffset, "Enso");
        AddDouble("TokkunMode", "FastTimeOffset", Description("config.TokkunMode.FastTimeOffset"),
            TokkunGameFastTimeOffset, "Enso");
        AddKeyBinding("TokkunMode", "P2LeftDonKey", Description("config.TokkunMode.P2LeftDonKey"), P2LeftDonKey, "Enso",
            "EnsoTest");
        AddKeyBinding("TokkunMode", "P2LeftKaKey", Description("config.TokkunMode.P2LeftKaKey"), P2LeftKaKey, "Enso",
            "EnsoTest");
        AddKeyBinding("TokkunMode", "P2RightDonKey", Description("config.TokkunMode.P2RightDonKey"), P2RightDonKey,
            "Enso", "EnsoTest");
        AddKeyBinding("TokkunMode", "P2RightKaKey", Description("config.TokkunMode.P2RightKaKey"), P2RightKaKey, "Enso",
            "EnsoTest");

        // Debug
        AddBool("Debug", "SaveRawSaveData", Description("config.Debug.SaveRawSaveData"), DebugSaveRawSaveData);
        AddBool("Debug", "ExportGameData", Description("config.Debug.ExportGameData"), DebugExportGameData);
        AddBool("Debug", "ExportMusicNames", Description("config.Debug.ExportMusicNames"), DebugExportMusicNames,
            "Boot");

        SetOptions("General.CustomTitleSceneEnterSceneName",
            ("", "modSettings.option.defaultMainMenu"),
            ("SongSelect", "modSettings.option.songSelect"),
            ("TrainingEntrance", "modSettings.option.trainingEntrance"));
        SetOptions("TokkunMode.OnSongEndBehaviour",
            ("ToSongStart", "modSettings.option.toSongStart"),
            ("ToLastResumePosition", "modSettings.option.toLastResumePosition"),
            ("PauseAtLastMeasure", "modSettings.option.pauseAtLastMeasure"));
        SetOptions("TokkunMode.OnPauseBehaviour",
            ("PauseAtCurrentPosition", "modSettings.option.pauseAtCurrentPosition"),
            ("ToLastPausePosition", "modSettings.option.toLastPausePosition"));

        MarkHotReloadable(
            "General.BetterBigHitSkipOnlineCheck",
            "General.AutoPlayRendaSpeed",
            "HitOffset.InvertColor",
            "HitOffset.RyoRange",
            "CustomPlayerName.Name",
            "BufferedInput.Enable",
            "BufferedInput.MaxBufferedInputCount",
            "TokkunMode.OnSongEndBehaviour",
            "TokkunMode.OnPauseBehaviour",
            "TokkunMode.SlowTimeOffset",
            "TokkunMode.FastTimeOffset",
            "TokkunMode.P2LeftDonKey",
            "TokkunMode.P2LeftKaKey",
            "TokkunMode.P2RightDonKey",
            "TokkunMode.P2RightKaKey");

        ConfigEntry.Load();
        KeyBindingConfigEntry.Load();
    }

    private static string Description(string key)
    {
        return key;
    }

    private static void SetOptions(string categoryKey, params (string Value, string LabelKey)[] options)
    {
        var item = AllItems.First(config => config.CategoryKey == categoryKey);
        item.Options = options.Select(option => new ConfigItemOption
        {
            Value = option.Value,
            LabelKey = option.LabelKey
        }).ToArray();
    }

    private static void MarkHotReloadable(params string[] categoryKeys)
    {
        var keys = categoryKeys.ToHashSet(StringComparer.Ordinal);
        foreach (var item in AllItems)
            item.HotReloadable = keys.Contains(item.CategoryKey);
    }

    private static void AddBool(string section, string key, string desc, ConfigEntry<bool> entry,
        params string[] scenes)
    {
        AllItems.Add(new ConfigItemMetadata
        {
            Section = section, KeyName = key, DescriptionKey = desc, Type = ConfigItemType.Bool,
            RelevantScenes = scenes,
            GetValue = () => entry.Value,
            SetValue = v => entry.Value = Convert.ToBoolean(v)
        });
    }

    private static void AddInt(string section, string key, string desc, ConfigEntry<int> entry, params string[] scenes)
    {
        AllItems.Add(new ConfigItemMetadata
        {
            Section = section, KeyName = key, DescriptionKey = desc, Type = ConfigItemType.Int,
            RelevantScenes = scenes,
            GetValue = () => entry.Value,
            SetValue = v => entry.Value = Convert.ToInt32(v)
        });
    }

    private static void AddFloat(string section, string key, string desc, ConfigEntry<float> entry,
        params string[] scenes)
    {
        AllItems.Add(new ConfigItemMetadata
        {
            Section = section, KeyName = key, DescriptionKey = desc, Type = ConfigItemType.Float,
            RelevantScenes = scenes,
            GetValue = () => entry.Value,
            SetValue = v => entry.Value = Convert.ToSingle(v)
        });
    }

    private static void AddDouble(string section, string key, string desc, ConfigEntry<double> entry,
        params string[] scenes)
    {
        AllItems.Add(new ConfigItemMetadata
        {
            Section = section, KeyName = key, DescriptionKey = desc, Type = ConfigItemType.Double,
            RelevantScenes = scenes,
            GetValue = () => entry.Value,
            SetValue = v => entry.Value = Convert.ToDouble(v)
        });
    }

    private static void AddString(string section, string key, string desc, ConfigEntry<string> entry,
        params string[] scenes)
    {
        AllItems.Add(new ConfigItemMetadata
        {
            Section = section, KeyName = key, DescriptionKey = desc, Type = ConfigItemType.String,
            RelevantScenes = scenes,
            GetValue = () => entry.Value,
            SetValue = v => entry.Value = Convert.ToString(v) ?? string.Empty
        });
    }

    private static void AddUInt(string section, string key, string desc, ConfigEntry<uint> entry,
        params string[] scenes)
    {
        AllItems.Add(new ConfigItemMetadata
        {
            Section = section, KeyName = key, DescriptionKey = desc, Type = ConfigItemType.UInt,
            RelevantScenes = scenes,
            GetValue = () => entry.Value,
            SetValue = v => entry.Value = Convert.ToUInt32(v)
        });
    }

    private static void AddKeyBinding(string section, string key, string desc, KeyBindingConfigEntry entry,
        params string[] scenes)
    {
        AllItems.Add(new ConfigItemMetadata
        {
            Section = section, KeyName = key, DescriptionKey = desc, Type = ConfigItemType.KeyBinding,
            RelevantScenes = scenes,
            GetValue = () => entry.Value,
            SetValue = v => entry.Value = (Key)v
        });
    }
}