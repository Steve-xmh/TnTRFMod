using System.Collections;
using System.Text;
using Il2CppInterop.Runtime;
using TnTRFMod.Config;
using TnTRFMod.Patches;
using TnTRFMod.Scenes;
using TnTRFMod.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Exception = System.Exception;
using Logger = TnTRFMod.Utils.Logger;

#if BEPINEX
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyInstance = HarmonyLib.Harmony;
#endif

#if MELONLOADER
using MelonLoader;
using HarmonyInstance = HarmonyLib.Harmony;
#endif

namespace TnTRFMod;

public class TnTrfMod
{
    public const string MOD_NAME = "TnTRFMod";
    public const string MOD_AUTHOR = "SteveXMH";
    public const string MOD_VERSION = "0.4.0";
#if BEPINEX
    public const string MOD_LOADER = "BepInEx";
#endif
#if MELONLOADER
    public const string MOD_LOADER = "MelonLoader";
#endif
    public const string MOD_GUID = "net.stevexmh.TnTRFMod";

    private readonly Hashtable _scenes = new();
    private HarmonyInstance _harmony;

    private MinimumLatencyAudioClient _minimumLatencyAudioClient;

#if BEPINEX
    internal Updater _updater;
#endif
    public ConfigEntry<bool> enableAutoDownloadSubscriptionSongs;

    public ConfigEntry<bool> enableBetterBigHitPatch;
    public ConfigEntry<bool> enableBufferedInputPatch;
    public ConfigEntry<bool> enableCustomDressAnimationMod;
    public ConfigEntry<bool> enableMinimumLatencyAudioClient;
    public ConfigEntry<bool> enableNearestNeighborOnpuPatch;
    public ConfigEntry<bool> enableNoShadowOnpuPatch;
    public ConfigEntry<bool> enableSkipBootScreenPatch;
    public ConfigEntry<bool> enableSkipRewardPatch;
    public ConfigEntry<uint> maxBufferedInputCount;

    public static TnTrfMod Instance { get; internal set; }

    private string sceneName { get; set; }

    public void Load(HarmonyInstance harmony)
    {
        _harmony = harmony;
        Console.OutputEncoding = Encoding.UTF8;
        Logger.Info("TnTRFMod has loaded!");

        // 默认启用的功能
        enableBetterBigHitPatch = ConfigEntry.Register("General", "EnableBetterBigHitPatch",
            "Whether to enable better Big Hit Patch, which will treat one side hit as a big hit.", true);
        enableSkipBootScreenPatch = ConfigEntry.Register("General", "EnableSkipBootScreenPatch",
            "Whether to enable Skip Boot Screen Patch.", true);
        enableSkipRewardPatch = ConfigEntry.Register("General", "EnableSkipRewardPatch",
            "Whether to enable Skip Reward Dialog Patch.", true);
        enableBufferedInputPatch = ConfigEntry.Register("General", "EnableBufferedInputPatch",
            "Whether to enable Buffered Input Patch.", true);
        enableMinimumLatencyAudioClient = ConfigEntry.Register("General", "EnableMinimumLatencyAudioClient",
            "Whether to enable Minimum Latency Audio Client, which can reduce the audio latency if possible.", true);
        // 默认禁用的功能
        enableNearestNeighborOnpuPatch = ConfigEntry.Register("General", "EnableNearestNeighborOnpuPatch",
            "Whether to enable Nearest Neighbor Onpu/Note Patch, this may make the notes look more pixelated.", false);
        enableNoShadowOnpuPatch = ConfigEntry.Register("General", "EnableNoShadowOnpuPatch",
            "Whether to enable No Shadow Onpu/Note Patch, this may reduce motion blur effect when notes are scrolling, but may also reduce the performance.",
            false);
        enableCustomDressAnimationMod = ConfigEntry.Register("General", "EnableCustomDressAnimationMod",
            "Enable a simple gui that can switch preview animation of don-chan when in dressing page.", false);
        enableAutoDownloadSubscriptionSongs = ConfigEntry.Register("General", "EnableAutoDownloadSubscriptionSongs",
            "Enable auto download subscription songs. (NOT FULLY TESTED)", false);

        maxBufferedInputCount = ConfigEntry.Register("BufferedInput", "MaxBufferedInputCount",
            "The maximum count of the buffered key input per side.", 5u);

        SceneManager.sceneLoaded +=
            DelegateSupport.ConvertDelegate<UnityAction<Scene, LoadSceneMode>>(OnSceneWasLoaded);
        SceneManager.sceneUnloaded +=
            DelegateSupport.ConvertDelegate<UnityAction<Scene>>(OnSceneWasUnloaded);

        SetupHarmony();
        RegisterScenes();

        try
        {
            if (enableMinimumLatencyAudioClient.Value)
            {
                _minimumLatencyAudioClient = new MinimumLatencyAudioClient();
                _minimumLatencyAudioClient.Start();
            }
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

    public void StartCoroutine(IEnumerator routine)
    {
#if BEPINEX
        _updater.StartCoroutine(routine.WrapToIl2Cpp());
#endif
#if MELONLOADER
        MelonCoroutines.Start(routine);
#endif
    }

    public void StartCoroutine(IEnumerable routine)
    {
#if BEPINEX
        // ReSharper disable once GenericEnumeratorNotDisposed
        _updater.StartCoroutine(ExecCoroutineWithIEnumerable(routine).WrapToIl2Cpp());
#endif
#if MELONLOADER
        MelonCoroutines.Start(ExecCoroutineWithIEnumerable(routine));
#endif
    }

    private static IEnumerator ExecCoroutineWithIEnumerable(IEnumerable routine)
    {
        yield return routine;
    }

//     public void StartCoroutine(Il2CppSystem.Collections.IEnumerator routine)
//     {
// #if BEPINEX
//         _updater.StartCoroutine(routine);
// #endif
// #if MELONLOADER
//         MelonCoroutines.Start(routine);
// #endif
//     }

    private void SetupHarmony()
    {
        var result = true;

        // _harmony.PatchAll();

        result &= PatchClass<BetterBigHitPatch>(enableBetterBigHitPatch);
        result &= PatchClass<SkipBootScreenPatch>(enableSkipBootScreenPatch);
        result &= PatchClass<SkipRewardPatch>(enableSkipRewardPatch);
        result &= PatchClass<NoShadowOnpuPatch>(enableNoShadowOnpuPatch);
        result &= PatchClass<NearestNeighborOnpuPatch>(enableNearestNeighborOnpuPatch);
        result &= PatchClass<BufferedNoteInputPatch>(enableBufferedInputPatch);
        result &= PatchClass<ReopenInviteDialogPatch>();

        if (result)
        {
            Logger.Info("Successfully injected all configured patches!");
        }
        else
        {
            Logger.Error("Due to some of the patches failed, reverting injected patches to ensure safety...");
            _harmony.UnpatchSelf();
        }
    }

    internal void OnUpdate()
    {
        if (sceneName != null && _scenes[sceneName] is IScene customScene) customScene.Update();
    }

    private void OnSceneWasLoaded(Scene scene, LoadSceneMode mode)
    {
        sceneName = scene.name;
        Logger.Info($"OnSceneWasLoaded {sceneName}");

        if (sceneName != null && _scenes[sceneName] is IScene customScene) customScene.Start();
    }

    private void OnSceneWasUnloaded(Scene scene)
    {
        Logger.Info($"OnSceneWasUnloaded {sceneName}");

        if (sceneName != null && _scenes[sceneName] is IScene customScene) customScene.Destroy();
    }

    private void RegisterScene<S>()
        where S : IScene, new()
    {
        Logger.Info($"Registering Scene {typeof(S).Name}");
        var s = new S();
        _scenes[s.SceneName] = s;
    }

    private bool PatchClass<T>(ConfigEntry<bool> configEntry = null)
    {
        try
        {
            if (configEntry is { Value: false }) return true;
            _harmony.PatchAll(typeof(T));
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

    private void RegisterScenes()
    {
        RegisterScene<DressUpModScene>();
        RegisterScene<TitleScene>();
        RegisterScene<EnsoScene>();
        RegisterScene<BootScene>();
        RegisterScene<EnsoNetworkScene>();
        RegisterScene<OnlineModJoinLobbyScene>();
    }

    internal class Updater : MonoBehaviour
    {
        private void Update()
        {
            Instance.OnUpdate();
        }
    }
}