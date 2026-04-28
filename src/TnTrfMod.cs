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
    public const string MOD_VERSION = "0.9.0";
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

    public static TnTrfMod Instance { get; internal set; }

    private string sceneName { get; set; }

    // "H:\SteamLibrary\steamapps\common\Taiko no Tatsujin Rhythm Festival\Taiko no Tatsujin Rhythm Festival_Data\Plugins\x86_64\LibTaiko.dll"
    [DllImport("Taiko no Tatsujin Rhythm Festival_Data/Plugins/x86_64/LibTaiko.dll", EntryPoint = "SetDebugLogFunc",
        CallingConvention = CallingConvention.StdCall)]
    private static extern void SetLibTaikoDebugLogFunc(OnLibTaikoLog func);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void OnLibTaikoLog(IntPtr msgBuffer);

    private static void SetupConfigs()
    {
        ModConfig.Register();
    }

    public void Load(HarmonyInstance harmony)
    {
        if (!Directory.Exists(Dir))
            Directory.CreateDirectory(Dir);
        I18n.Load();
        SetupConfigs();
        if (!ModConfig.EnableMod.Value)
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
        if (ModConfig.ModifyMeasuresCapacity.Value > 300)
            LibTaikoPatches.InitExpandCSyousetsu(ModConfig.ModifyMeasuresCapacity.Value);
        _ = SongAliasTable.ReloadAliasTable();

        SceneManager.sceneLoaded +=
            DelegateSupport.ConvertDelegate<UnityAction<Scene, LoadSceneMode>>(OnSceneWasLoaded);
        SceneManager.sceneUnloaded +=
            DelegateSupport.ConvertDelegate<UnityAction<Scene>>(OnSceneWasUnloaded);

        SetupHarmony();
        RegisterBuiltinScenes();

        if (ModConfig.EnableHighPrecisionTimerPatch.Value) HighPrecisionTimerPatch.Apply();

        // try
        // {
        //     if (enableCriWareExclusiveModePatch.Value) CriWareEnableExclusiveModePatch.Apply();
        // }
        // catch (Exception e)
        // {
        //     Logger.Error("Failed to apply CriWareEnableExclusiveModePatch:");
        //     Logger.Error(e);
        // }

        try
        {
            // if (enableCriWareExclusiveModePatch.Value)
            //     Logger.Info("Skipping MinimumLatencyAudioClient because CriWare exclusive mode hook is enabled.");
            if (ModConfig.EnableMinimumLatencyAudioClient.Value)
                _minimumLatencyAudioClient.Start();
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
        // CriWareEnableExclusiveModePatch.Reset();
        return false;
    }

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

        result &= PatchClass<BetterBigHitPatch>(ModConfig.EnableBetterBigHitPatch);
        result &= PatchClass<SkipBootScreenPatch>();
        result &= PatchClass<SkipRewardPatch>(ModConfig.EnableSkipRewardPatch);
        result &= PatchClass<NoShadowOnpuPatch>(ModConfig.EnableNoShadowOnpuPatch);
        result &= PatchClass<NearestNeighborOnpuPatch>(ModConfig.EnableNearestNeighborOnpuPatch);
        result &= PatchClass<BufferedNoteInputPatch>();
        result &= PatchClass<ForcePlayMusicPatch>(ModConfig.EnableLouderSongPatch);
        result &= PatchClass<CustomPlayerNamePatch>(ModConfig.EnableCustomPlayerName);
        result &= PatchClass<AutoDownloadSubscriptionSongs>(ModConfig.EnableAutoDownloadSubscriptionSongs);
        result &= PatchClass<EnsoGameBasePatch>();
        result &= PatchClass<LibTaikoPatches>();
        // result &= PatchClass<SmoothEnsoGamePatch>();
        result &= PatchClass<RefinedDifficultyButtonsPatch>();
        result &= PatchClass<FumenPostProcessingPatch>();
        result &= PatchClass<CustomTitleSceneEnterPatch>();
        // result &= PatchClass<HiResDonImagePatch>();
        result &= PatchClass<InstantRelayPatch>(ModConfig.EnableInstantRelayPatch);
        result &= PatchClass<ScoreRankIconPatch>(ModConfig.EnableScoreRankIcon);
        // result &= PatchClass<CustomSongSaveDataPatch>(enableCustomSongs);
        // result &= PatchClass<CustomSongLoaderPatch>(enableCustomSongs);
        result &= PatchClass<TokkunGamePatch>(ModConfig.EnableTokkunGamePatch);
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
        var (label, color, isError) = type switch
        {
            LogType.Error => ("[Unity][Error]:      ", "\e[1;31m", true),
            LogType.Assert => ("[Unity][Assert]:    ", "\e[1;31m", true),
            LogType.Warning => ("[Unity][Warning]:   ", "\e[1;33m", true),
            LogType.Log => ("[Unity][Info]:      ", null, false),
            LogType.Exception => ("[Unity][Exception]: ", "\e[1;91m", true),
            _ => ("[Unity][Unknown]:   ", null, false)
        };

        if (color != null) Console.Out.Write(color);
        Console.Out.Write(label);
        Console.Out.WriteLine(logLine.Trim());

        if (isError && exception.Trim().Length > 0)
        {
            const string indent = "                    ";
            const string indentLayer = "                      ";
            Console.Out.Write(indent);
            Console.Out.WriteLine("Stacktrace:");
            foreach (var line in exception.Trim().Split('\n'))
            {
                Console.Out.Write(indentLayer);
                Console.Out.WriteLine(line);
            }
        }

        if (color != null) Console.Out.Write("\e[0m");
    }

    public readonly ConcurrentQueue<Action> RunOnMainThread = new();

    public void OnUpdate()
    {
        if (!ModConfig.EnableMod.Value) return;
        if (!RunOnMainThread.IsEmpty)
            while (RunOnMainThread.TryDequeue(out var action))
                action?.Invoke();

        if (!_scenes.TryGetValue(sceneName, out var scenes)) return;

        foreach (var scene in scenes) scene.Update();
    }

    private static void ForceCollectAllGenerations()
    {
        GC.Collect(0, GCCollectionMode.Forced, true, true);
        GC.Collect(1, GCCollectionMode.Forced, true, true);
        GC.Collect(2, GCCollectionMode.Forced, true, true);
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
            GCSettings.LatencyMode = GCLatencyMode.LowLatency;
            ForceCollectAllGenerations();
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
            ForceCollectAllGenerations();
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