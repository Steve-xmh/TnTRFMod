using System.Collections;
using System.Collections.Concurrent;
using System.Runtime;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;
using TnTRFMod.Config;
using TnTRFMod.Patches;
using TnTRFMod.Scenes;
using TnTRFMod.Ui;
using TnTRFMod.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Exception = System.Exception;
using Logger = TnTRFMod.Utils.Logger;
using Il2CppIEnumerator = Il2CppSystem.Collections.IEnumerator;

#if BEPINEX
using HarmonyInstance = HarmonyLib.Harmony;
#endif

#if MELONLOADER
using HarmonyInstance = HarmonyLib.Harmony;
#endif

namespace TnTRFMod;

public class TnTrfMod
{
    public const string MOD_NAME = "TnTRFMod";
    public const string MOD_AUTHOR = "SteveXMH";
    public const string MOD_VERSION = "0.8.1";
#if BEPINEX
    public const string MOD_LOADER = "BepInEx";
#endif
#if MELONLOADER
    public const string MOD_LOADER = "MelonLoader";
#endif
    public const string MOD_GUID = "net.stevexmh.TnTRFMod";

    private readonly Dictionary<string, HashSet<IScene>> _scenes = new();
    private HarmonyInstance Harmony;

    private readonly MinimumLatencyAudioClient _minimumLatencyAudioClient = new();

    public static readonly string Dir = Path.GetFullPath(Path.Join(Application.dataPath, "../TnTRFMod"));

    internal CoroutineRunner _runner;

    public ConfigEntry<bool> enableAutoDownloadSubscriptionSongs = ConfigEntry<bool>.Noop;

    public ConfigEntry<bool> enableBetterBigHitPatch = ConfigEntry<bool>.Noop;
    public ConfigEntry<bool> betterBigHitSkipOnlineCheck = ConfigEntry<bool>.Noop;
    public ConfigEntry<bool> enableBufferedInputPatch = ConfigEntry<bool>.Noop;
    public ConfigEntry<bool> enableCustomDressAnimationMod = ConfigEntry<bool>.Noop;
    public ConfigEntry<bool> enableMinimumLatencyAudioClient = ConfigEntry<bool>.Noop;
    public ConfigEntry<bool> enableHitStatsPanelPatch = ConfigEntry<bool>.Noop;
    public ConfigEntry<bool> enableHighPrecisionTimerPatch = ConfigEntry<bool>.Noop;
    public ConfigEntry<bool> enableHitOffset = ConfigEntry<bool>.Noop;
    public ConfigEntry<bool> hitOffsetInvertColor = ConfigEntry<bool>.Noop;
    public ConfigEntry<float> hitOffsetRyoRange = ConfigEntry<float>.Noop;
    public ConfigEntry<bool> enableScoreRankIcon = ConfigEntry<bool>.Noop;
    public ConfigEntry<bool> enableOnpuTextRail = ConfigEntry<bool>.Noop;
    public ConfigEntry<bool> enableMod = ConfigEntry<bool>.Noop;
    public ConfigEntry<bool> enableNearestNeighborOnpuPatch = ConfigEntry<bool>.Noop;
    public ConfigEntry<bool> enableNoShadowOnpuPatch = ConfigEntry<bool>.Noop;
    public ConfigEntry<bool> enableSkipBootScreenPatch = ConfigEntry<bool>.Noop;
    public ConfigEntry<bool> enableSkipRewardPatch = ConfigEntry<bool>.Noop;
    public ConfigEntry<bool> enableLouderSongPatch = ConfigEntry<bool>.Noop;
    public ConfigEntry<int> maxBufferedInputCount = ConfigEntry<int>.Noop;
    public ConfigEntry<float> autoPlayRendaSpeed = ConfigEntry<float>.Noop;
    public ConfigEntry<bool> enableTatakonKeyboardSongSelect = ConfigEntry<bool>.Noop;
    public ConfigEntry<bool> enableInstantRelayPatch = ConfigEntry<bool>.Noop;
    public ConfigEntry<string> customTitleSceneEnterSceneName = ConfigEntry<string>.Noop;

    // 特训模式
    public ConfigEntry<bool> enableTokkunGamePatch = ConfigEntry<bool>.Noop;
    public ConfigEntry<string> tokkunGameOnSongEndBehaviour = ConfigEntry<string>.Noop;
    public ConfigEntry<string> tokkunGameOnPauseBehaviour = ConfigEntry<string>.Noop;
    public ConfigEntry<double> tokkunGameSlowTimeOffset = ConfigEntry<double>.Noop;
    public ConfigEntry<double> tokkunGameFastTimeOffset = ConfigEntry<double>.Noop;

    // 调试功能
    public ConfigEntry<bool> debugSaveRawSaveData = ConfigEntry<bool>.Noop;
    public ConfigEntry<bool> debugExportGameData = ConfigEntry<bool>.Noop;
    public ConfigEntry<bool> debugExportMusicNames = ConfigEntry<bool>.Noop;

    // 自定义玩家名称功能
    public ConfigEntry<bool> enableCustomPlayerName = ConfigEntry<bool>.Noop;
    public ConfigEntry<string> customPlayerName = ConfigEntry<string>.Noop;

    // 直播点歌功能
    public ConfigEntry<bool> enableBilibiliLiveStreamSongRequest = ConfigEntry<bool>.Noop;
    public ConfigEntry<uint> bilibiliLiveStreamSongRoomId = ConfigEntry<uint>.Noop;
    public ConfigEntry<string> bilibiliLiveStreamSongToken = ConfigEntry<string>.Noop;

    // 按键映射
    public KeyBindingConfigEntry p2LeftDonKey = KeyBindingConfigEntry.Noop;
    public KeyBindingConfigEntry p2LeftKaKey = KeyBindingConfigEntry.Noop;
    public KeyBindingConfigEntry p2RightDonKey = KeyBindingConfigEntry.Noop;
    public KeyBindingConfigEntry p2RightKaKey = KeyBindingConfigEntry.Noop;

    public static TnTrfMod Instance { get; internal set; }

    private string sceneName { get; set; }

    // "H:\SteamLibrary\steamapps\common\Taiko no Tatsujin Rhythm Festival\Taiko no Tatsujin Rhythm Festival_Data\Plugins\x86_64\LibTaiko.dll"
    [DllImport("Taiko no Tatsujin Rhythm Festival_Data/Plugins/x86_64/LibTaiko.dll", EntryPoint = "SetDebugLogFunc",
        CallingConvention = CallingConvention.StdCall)]
    private static extern void SetLibTaikoDebugLogFunc(OnLibTaikoLog func);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void OnLibTaikoLog(IntPtr msgBuffer);

    private void SetupConfigs()
    {
        // 默认启用的功能
        enableMod = ConfigEntry.Register("General", "Enabled",
            "config.Enabled", true);
        enableBetterBigHitPatch = ConfigEntry.Register("General", "EnableBetterBigHitPatch",
            "config.EnableBetterBigHitPatch", true);
        betterBigHitSkipOnlineCheck = ConfigEntry.Register("General", "BetterBigHitSkipOnlineCheck",
            "config.BetterBigHitSkipOnlineCheck", false);
        enableSkipBootScreenPatch = ConfigEntry.Register("General", "EnableSkipBootScreenPatch",
            "config.EnableSkipBootScreenPatch", true);
        enableSkipRewardPatch = ConfigEntry.Register("General", "EnableSkipRewardPatch",
            "config.EnableSkipRewardPatch", true);
        enableMinimumLatencyAudioClient = ConfigEntry.Register("General", "EnableMinimumLatencyAudioClient",
            "config.EnableMinimumLatencyAudioClient", true);
        enableAutoDownloadSubscriptionSongs = ConfigEntry.Register("General", "EnableAutoDownloadSubscriptionSongs",
            "config.EnableAutoDownloadSubscriptionSongs", true);
        enableOnpuTextRail = ConfigEntry.Register("General", "EnableOnpuTextRail",
            "config.EnableOnpuTextRail", true);
        enableHighPrecisionTimerPatch = ConfigEntry.Register("General", "EnableHighPrecisionTimerPatch",
            "config.EnableHighPrecisionTimerPatch", true);
        // 默认禁用的功能
        enableNearestNeighborOnpuPatch = ConfigEntry.Register("General", "EnableNearestNeighborOnpuPatch",
            "config.EnableNearestNeighborOnpuPatch", false);
        enableNoShadowOnpuPatch = ConfigEntry.Register("General", "EnableNoShadowOnpuPatch",
            "config.EnableNoShadowOnpuPatch",
            false);
        enableCustomDressAnimationMod = ConfigEntry.Register("General", "EnableCustomDressAnimationMod",
            "config.EnableCustomDressAnimationMod", false);
        enableHitStatsPanelPatch = ConfigEntry.Register("General", "EnableHitStatsPanelPatch",
            "config.EnableHitStatsPanelPatch",
            false);
        enableLouderSongPatch = ConfigEntry.Register("General", "EnableLouderSongPatch",
            "config.EnableLouderSongPatch",
            false);
        enableScoreRankIcon = ConfigEntry.Register("General", "EnableScoreRankIcon",
            "config.EnableScoreRankIcon",
            false);
        enableTatakonKeyboardSongSelect = ConfigEntry.Register("General", "EnableTatakonKeyboardSongSelect",
            "config.EnableTatakonKeyboardSongSelect", false);
        // 敲击时差功能
        enableHitOffset = ConfigEntry.Register("HitOffset", "Enable",
            "config.HitOffset.Enable",
            false);
        hitOffsetInvertColor = ConfigEntry.Register("HitOffset", "InvertColor",
            "config.HitOffset.InvertColor",
            false);
        hitOffsetRyoRange = ConfigEntry.Register("HitOffset", "RyoRange",
            "config.HitOffset.RyoRange",
            -1f);
        // 直播点歌功能
        enableBilibiliLiveStreamSongRequest = ConfigEntry.Register("BilibiliLiveStreamSongRequest", "Enable",
            "config.BilibiliLiveStreamSongRequest.Enable", false);
        bilibiliLiveStreamSongRoomId = ConfigEntry.Register("BilibiliLiveStreamSongRequest", "RoomId",
            "config.BilibiliLiveStreamSongRequest.RoomId", 0u);
        bilibiliLiveStreamSongToken = ConfigEntry.Register("BilibiliLiveStreamSongRequest", "Token",
            "config.BilibiliLiveStreamSongRequest.Token",
            "");

        customTitleSceneEnterSceneName = ConfigEntry.Register("General", "CustomTitleSceneEnterSceneName",
            "config.CustomTitleSceneEnterSceneName", "");

        enableCustomPlayerName =
            ConfigEntry.Register("CustomPlayerName", "Enable", "config.CustomPlayerName.Enable", false);
        customPlayerName = ConfigEntry.Register("CustomPlayerName", "Name", "config.CustomPlayerName.Name", "Don-chan");
        enableInstantRelayPatch =
            ConfigEntry.Register("General", "EnableInstantRelayPatch", "config.EnableInstantRelayPatch", true);

        enableBufferedInputPatch = ConfigEntry.Register("BufferedInput", "Enable",
            "config.BufferedInput.Enable", true);
        maxBufferedInputCount = ConfigEntry.Register("BufferedInput", "MaxBufferedInputCount",
            "config.BufferedInput.MaxBufferedInputCount", 5);

        autoPlayRendaSpeed = ConfigEntry.Register("General", "AutoPlayRendaSpeed",
            "config.AutoPlayRendaSpeed",
            30f);

        // 特训模式
        enableTokkunGamePatch = ConfigEntry.Register("TokkunMode", "Enable",
            "config.TokkunMode.Enable", false);
        tokkunGameOnSongEndBehaviour = ConfigEntry.Register("TokkunMode", "OnSongEndBehaviour",
            "config.TokkunMode.OnSongEndBehaviour", "ToSongStart");
        tokkunGameOnPauseBehaviour = ConfigEntry.Register("TokkunMode", "OnPauseBehaviour",
            "config.TokkunMode.OnPauseBehaviour", "PauseAtCurrentPosition");
        tokkunGameSlowTimeOffset = ConfigEntry.Register("TokkunMode", "SlowTimeOffset",
            "config.TokkunMode.SlowTimeOffset", -100.0);
        tokkunGameFastTimeOffset = ConfigEntry.Register("TokkunMode", "FastTimeOffset",
            "config.TokkunMode.FastTimeOffset", 0.0);

        // 调试相关功能
        debugSaveRawSaveData = ConfigEntry.Register("Debug", "SaveRawSaveData",
            "config.Debug.SaveRawSaveData", false);
        debugExportGameData = ConfigEntry.Register("Debug", "ExportGameData",
            "config.Debug.ExportGameData", false);
        debugExportMusicNames = ConfigEntry.Register("Debug", "ExportMusicNames",
            "config.Debug.ExportMusicNames", false);

        // 按键映射

        p2LeftDonKey =
            KeyBindingConfigEntry.Register("TokkunMode", "P2LeftDonKey", "config.TokkunMode.P2LeftDonKey", Key.X);
        p2LeftKaKey =
            KeyBindingConfigEntry.Register("TokkunMode", "P2LeftKaKey", "config.TokkunMode.P2LeftKaKey", Key.Z);
        p2RightDonKey =
            KeyBindingConfigEntry.Register("TokkunMode", "P2RightDonKey", "config.TokkunMode.P2RightDonKey", Key.C);
        p2RightKaKey =
            KeyBindingConfigEntry.Register("TokkunMode", "P2RightKaKey", "config.TokkunMode.P2RightKaKey", Key.V);

        ConfigEntry.Load();
        KeyBindingConfigEntry.Load();
    }

    private IEnumerator EmptyEnumerator()
    {
        yield break;
    }

    public void Load(HarmonyInstance harmony)
    {
        StartCoroutine(EmptyEnumerator());
        if (!Directory.Exists(Dir))
            Directory.CreateDirectory(Dir);
        I18n.Load();
        SetupConfigs();
        if (!enableMod.Value)
        {
            Logger.Warn("TnTRFMod has disabled!");
            return;
        }

        Harmony = harmony;
        Logger.Info("TnTRFMod has loaded!");

        SetLibTaikoDebugLogFunc(buffer =>
        {
            var msg = Marshal.PtrToStringAnsi(buffer);
            Console.Out.Write(msg);
        });
        _ = SongAliasTable.ReloadAliasTable();

        SceneManager.sceneLoaded +=
            DelegateSupport.ConvertDelegate<UnityAction<Scene, LoadSceneMode>>(OnSceneWasLoaded);
        SceneManager.sceneUnloaded +=
            DelegateSupport.ConvertDelegate<UnityAction<Scene>>(OnSceneWasUnloaded);

        SetupHarmony();
        RegisterBuiltinScenes();

        if (enableHighPrecisionTimerPatch.Value) HighPrecisionTimerPatch.Apply();

        try
        {
            if (enableMinimumLatencyAudioClient.Value) _minimumLatencyAudioClient.Start();
        }
        catch (Exception e)
        {
            Logger.Error("Failed to start MinimumLatencyAudioClient:");
            Logger.Error(e);
        }
    }

    public bool Unload()
    {
        _minimumLatencyAudioClient.Stop();
        return false;
    }

//     public void StartCoroutine(Task task)
//     {
// #if BEPINEX
//         _updater.StartCoroutine();
// #endif
// #if MELONLOADER
//         MelonCoroutines.Start(routine);
// #endif
//     }

    public void StartCoroutine(IEnumerator routine)
    {
        _runner.RunCoroutine(routine);
    }

    public void StartCoroutine(Il2CppIEnumerator routine)
    {
        _runner.RunCoroutine(routine);
    }

    public void StartCoroutine(IEnumerable routine)
    {
        _runner.RunCoroutine(ExecCoroutineWithIEnumerable(routine));
    }

    private static IEnumerator ExecCoroutineWithIEnumerable(IEnumerable routine)
    {
        yield return routine;
    }

    private void SetupHarmony()
    {
        var result = true;

        // _harmony.PatchAll();

        result &= PatchClass<BetterBigHitPatch>(enableBetterBigHitPatch);
        result &= PatchClass<SkipBootScreenPatch>(enableSkipBootScreenPatch);
        result &= PatchClass<SkipRewardPatch>(enableSkipRewardPatch);
        result &= PatchClass<NoShadowOnpuPatch>(enableNoShadowOnpuPatch);
        result &= PatchClass<NearestNeighborOnpuPatch>(enableNearestNeighborOnpuPatch);
        result &= PatchClass<BufferedNoteInputPatch>();
        result &= PatchClass<ForcePlayMusicPatch>(enableLouderSongPatch);
        result &= PatchClass<CustomPlayerNamePatch>(enableCustomPlayerName);
        result &= PatchClass<AutoDownloadSubscriptionSongs>(enableAutoDownloadSubscriptionSongs);
        result &= PatchClass<EnsoGameBasePatch>();
        result &= PatchClass<LibTaikoPatches>();
        result &= PatchClass<SmoothEnsoGamePatch>();
        result &= PatchClass<RefinedDifficultyButtonsPatch>();
        result &= PatchClass<FumenPostProcessingPatch>();
        result &= PatchClass<CustomTitleSceneEnterPatch>();
        // result &= PatchClass<HiResDonImagePatch>();
        result &= PatchClass<InstantRelayPatch>(enableInstantRelayPatch);
        result &= PatchClass<ScoreRankIconPatch>(enableScoreRankIcon);
        // result &= PatchClass<CustomSongSaveDataPatch>(enableCustomSongs);
        // result &= PatchClass<CustomSongLoaderPatch>(enableCustomSongs);
        result &= PatchClass<TokkunGamePatch>(enableTokkunGamePatch);
        // CustomSongLoaderPatch.PatchLibTaiko();

        Application.s_LogCallbackHandler = new Action<string, string, LogType>(UnityLogCallback);

        if (result)
        {
            Logger.Info("Successfully injected all configured patches!");
        }
        else
        {
            Logger.Error("Due to some of the patches failed, reverting injected patches to ensure safety...");
            Harmony.UnpatchSelf();
        }
    }

    public void UnityLogCallback(string logLine, string exception, LogType type)
    {
        switch (type)
        {
            case LogType.Error:
                Console.Out.Write("\e[1;31m");
                Console.Out.Write("[Unity][Error]:      ");
                break;
            case LogType.Assert:
                Console.Out.Write("\e[1;31m");
                Console.Out.Write("[Unity][Assert]:    ");
                break;
            case LogType.Warning:
                Console.Out.Write("\e[1;33m");
                Console.Out.Write("[Unity][Warning]:   ");
                break;
            case LogType.Log:
                Console.Out.Write("[Unity][Info]:      ");
                break;
            case LogType.Exception:
                Console.Out.Write("\e[1;91m");
                Console.Out.Write("[Unity][Exception]: ");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        Console.Out.WriteLine(logLine.Trim());
        if (type != LogType.Log && exception.Trim().Length > 0)
        {
            const string indent = "                    ";
            const string indentLayer = "                      ";
            Console.Out.Write(indent);
            Console.Out.WriteLine("Stacktrace:");
            foreach (var line in exception.Trim().Split("\n"))
            {
                Console.Out.Write(indentLayer);
                Console.Out.WriteLine(line);
            }
        }

        Console.Out.Write("\e[0m");
    }

    public readonly ConcurrentQueue<Action> RunOnMainThread = new();

    public void OnUpdate()
    {
        if (!enableMod.Value) return;
        if (!RunOnMainThread.IsEmpty)
            while (RunOnMainThread.TryDequeue(out var action))
                action?.Invoke();

        if (!_scenes.TryGetValue(sceneName, out var scenes)) return;
        var shouldInvokeLowLatencyGC = false;
        foreach (var scene in scenes)
        {
            scene.Update();
            shouldInvokeLowLatencyGC |= scene.LowLatencyMode;
        }

        if (shouldInvokeLowLatencyGC) GC.Collect(0, GCCollectionMode.Forced);
    }

    private void OnSceneWasLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Equals(scene, null)) return;
        sceneName = scene.name;
        Logger.Info($"OnSceneWasLoaded {sceneName}");
        var time = DateTime.Now;

        Common.Init();
        Common.InitLocal();

        if (!_scenes.TryGetValue(sceneName, out var scenes)) return;
        var shouldInvokeLowLatencyGC = false;
        foreach (var customScene in scenes)
        {
            customScene.Start();
            shouldInvokeLowLatencyGC |= customScene.LowLatencyMode;
        }

        if (shouldInvokeLowLatencyGC)
        {
            GC.Collect(0, GCCollectionMode.Forced, true, true);
            GC.Collect(1, GCCollectionMode.Forced, true, true);
            GC.Collect(2, GCCollectionMode.Forced, true, true);
        }

        Logger.Info($"OnSceneWasLoaded {sceneName} ended, took {(DateTime.Now - time).TotalMilliseconds:N0}ms");
    }

    private void OnSceneWasUnloaded(Scene scene)
    {
        if (Equals(scene, null)) return;
        sceneName = "";
        var unloadedSceneName = scene.name;
        Logger.Info($"OnSceneWasUnloaded {unloadedSceneName}");
        var time = DateTime.Now;

        if (!_scenes.TryGetValue(unloadedSceneName, out var scenes)) return;
        var shouldInvokeLowLatencyGC = false;
        foreach (var customScene in scenes)
        {
            customScene.Destroy();
            shouldInvokeLowLatencyGC |= customScene.LowLatencyMode;
        }

        if (shouldInvokeLowLatencyGC)
        {
            GCSettings.LatencyMode = GCLatencyMode.Interactive;
            GC.Collect(0, GCCollectionMode.Forced, true, true);
            GC.Collect(1, GCCollectionMode.Forced, true, true);
            GC.Collect(2, GCCollectionMode.Forced, true, true);
        }

        Logger.Info(
            $"OnSceneWasUnloaded {unloadedSceneName} ended, took {(DateTime.Now - time).TotalMilliseconds:N0}ms");
    }

    public void RegisterScene<S>()
        where S : IScene, new()
    {
        Logger.Info($"Registering Scene {typeof(S).Name}");
        var s = new S();
        s.Init();


        if (_scenes.TryGetValue(s.SceneName, out var scenes))
        {
            if (!scenes.Add(s)) Logger.Warn($"Scene {s.GetType().FullName} already registered");
        }
        else
        {
            _scenes[s.SceneName] = new HashSet<IScene> { s };
        }
    }

    public string GetSceneName()
    {
        return sceneName;
    }

    private bool PatchClass<T>(ConfigEntry<bool> configEntry = null)
    {
        try
        {
            if (configEntry is { Value: false }) return true;
            Harmony.PatchAll(typeof(T));
            Logger.Info($"Injected \"{typeof(T).Name}\" Patch");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Patch \"{typeof(T).Name}\" failed to inject:");
            Logger.Error(ex.Message);
            return false;
        }
    }

    private void RegisterBuiltinScenes()
    {
        RegisterScene<DressUpModScene>();
        RegisterScene<TitleScene>();
        RegisterScene<EnsoScene>();
        RegisterScene<EnsoTestScene>();
        RegisterScene<BootScene>();
        RegisterScene<EnsoNetworkScene>();
        RegisterScene<OnlineModJoinLobbyScene>();
        RegisterScene<SongSelectScene>();
    }

    internal interface CoroutineRunner
    {
        void RunCoroutine(IEnumerator routine);
        void RunCoroutine(Il2CppIEnumerator routine);
        void RunCoroutine(IEnumerable routine);
    }
}