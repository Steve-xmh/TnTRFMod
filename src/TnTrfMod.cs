using System.Collections;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Il2CppInterop.Runtime;
using TnTRFMod.Patches;
using TnTRFMod.Scenes;
using TnTRFMod.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Exception = System.Exception;

namespace TnTRFMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class TnTrfMod : BasePlugin
{
    internal new static ManualLogSource Log;

    private readonly Hashtable _scenes = new();
    private Harmony _harmony;

    private MinimumLatencyAudioClient _minimumLatencyAudioClient;

    private Updater _updater;

    public ConfigEntry<bool> enableBetterBigHitPatch;
    public ConfigEntry<bool> enableBufferedInputPatch;
    public ConfigEntry<bool> enableCustomDressAnimationMod;
    public ConfigEntry<bool> enableNearestNeighborOnpuPatch;
    public ConfigEntry<bool> enableNoShadowOnpuPatch;
    public ConfigEntry<bool> enableAutoDownloadSubscriptionSongs;
    public ConfigEntry<bool> enableMinimumLatencyAudioClient;
    public ConfigEntry<bool> enableSkipBootScreenPatch;
    public ConfigEntry<bool> enableSkipRewardPatch;
    public ConfigEntry<uint> maxBufferedInputCount;

    public static TnTrfMod Instance { get; private set; }

    public string sceneName { get; set; }

    public override void Load()
    {
        Console.OutputEncoding = Encoding.Unicode;
        Instance = this;
        Log = base.Log;
        Log.LogInfo($"TnTRFMod has loaded!");

        // 默认启用的功能
        enableBetterBigHitPatch = Config.Bind("General", "EnableBetterBigHitPatch", true,
            "Whether to enable better Big Hit Patch, which will treat one side hit as a big hit.");
        enableSkipBootScreenPatch = Config.Bind("General", "EnableSkipBootScreenPatch", true,
            "Whether to enable Skip Boot Screen Patch.");
        enableSkipRewardPatch = Config.Bind("General", "EnableSkipRewardPatch", true,
            "Whether to enable Skip Reward Dialog Patch.");
        enableBufferedInputPatch = Config.Bind("General", "EnableBufferedInputPatch", true,
            "Whether to enable Buffered Input Patch.");
        enableMinimumLatencyAudioClient = Config.Bind("General", "EnableMinimumLatencyAudioClient", true,
            "Whether to enable Minimum Latency Audio Client, which can reduce the audio latency if possible.");
        // 默认禁用的功能
        enableNearestNeighborOnpuPatch = Config.Bind("General", "EnableNearestNeighborOnpuPatch", false,
            "Whether to enable Nearest Neighbor Onpu/Note Patch, this may make the notes look more pixelated.");
        enableNoShadowOnpuPatch = Config.Bind("General", "EnableNoShadowOnpuPatch", false,
            "Whether to enable No Shadow Onpu/Note Patch, this may reduce motion blur effect when notes are scrolling, but may also reduce the performance.");
        enableCustomDressAnimationMod = Config.Bind("General", "EnableCustomDressAnimationMod", false,
            "Enable a simple gui that can switch preview animation of don-chan when in dressing page.");
        enableAutoDownloadSubscriptionSongs = Config.Bind("General", "EnableAutoDownloadSubscriptionSongs", false,
            "Enable auto download subscription songs. (NOT FULLY TESTED)");

        maxBufferedInputCount = Config.Bind("BufferedInput", "MaxBufferedInputCount", 30u,
            "The maximum count of the buffered key input per side.");

        SceneManager.sceneLoaded +=
            DelegateSupport.ConvertDelegate<UnityAction<Scene, LoadSceneMode>>(OnSceneWasLoaded);
        SceneManager.sceneUnloaded +=
            DelegateSupport.ConvertDelegate<UnityAction<Scene>>(OnSceneWasUnloaded);

        SetupHarmony();
        RegisterScene<DressUpModScene>();
        RegisterScene<TitleScene>();
        RegisterScene<EnsoScene>();
        RegisterScene<BootScene>();
        _updater = AddComponent<Updater>();

        _minimumLatencyAudioClient = new MinimumLatencyAudioClient();
        _minimumLatencyAudioClient.Start();
    }

    public override bool Unload()
    {
        _minimumLatencyAudioClient.Stop();
        return false;
    }

    public void StartCoroutine(IEnumerator routine)
    {
        _updater.StartCoroutine(routine.WrapToIl2Cpp());
    }

    public void StartCoroutine(IEnumerable routine)
    {
        _updater.StartCoroutine(routine.WrapToIl2Cpp().GetEnumerator());
    }

    public void StartCoroutine(Il2CppSystem.Collections.IEnumerator routine)
    {
        _updater.StartCoroutine(routine);
    }

    private void SetupHarmony()
    {
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        var result = true;

        // _harmony.PatchAll();

        result &= PatchClass<BetterBigHitPatch>(enableBetterBigHitPatch);
        result &= PatchClass<SkipBootScreenPatch>(enableSkipBootScreenPatch);
        result &= PatchClass<SkipRewardPatch>(enableSkipRewardPatch);
        result &= PatchClass<NoShadowOnpuPatch>(enableNoShadowOnpuPatch);
        result &= PatchClass<NearestNeighborOnpuPatch>(enableNearestNeighborOnpuPatch);
        result &= PatchClass<BufferedNoteInputPatch>(enableBufferedInputPatch);

        if (result)
        {
            Log.LogInfo($"Successfully injected all configured patches!");
        }
        else
        {
            Log.LogError($"Due to some of the patches failed, reverting injected patches to ensure safety...");
            _harmony.UnpatchSelf();
        }
    }

    private void OnUpdate()
    {
        if (sceneName != null && _scenes[sceneName] is IScene customScene) customScene.Update();
    }

    private void OnSceneWasLoaded(Scene scene, LoadSceneMode mode)
    {
        sceneName = scene.name;
        Log.LogInfo($"OnSceneWasLoaded {sceneName}");

        if (sceneName != null && _scenes[sceneName] is IScene customScene) customScene.Start();
    }

    private void OnSceneWasUnloaded(Scene scene)
    {
        Log.LogInfo($"OnSceneWasUnloaded {sceneName}");

        if (sceneName != null && _scenes[sceneName] is IScene customScene) customScene.Destroy();
    }

    private void RegisterScene<S>()
        where S : IScene, new()
    {
        Log.LogInfo($"Registering Scene {typeof(S).Name}");
        var s = new S();
        _scenes[s.SceneName] = s;
    }

    private bool PatchClass<T>(ConfigEntry<bool> configEntry)
    {
        try
        {
            if (configEntry == null) return true;
            if (!configEntry.Value) return true;
            _harmony.PatchAll(typeof(T));
            Log.LogInfo($"Injected \"{typeof(T).Name}\" Patch");
            return true;
        }
        catch (Exception ex)
        {
            Log.LogError($"Patch \"{typeof(T).Name}\" failed to inject:");
            Log.LogError(ex.Message);
            return false;
        }
    }

    private class Updater : MonoBehaviour
    {
        private void Update()
        {
            Instance.OnUpdate();
        }
    }
}