using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using TnTRFMod.Patches;
using TnTRFMod.Ui.Scenes;
using TnTRFMod.Ui.Widgets;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Exception = System.Exception;

namespace TnTRFMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class TnTrfMod : BasePlugin
{
    internal new static ManualLogSource Log;
    private Harmony _harmony;

    public ConfigEntry<bool> enableBetterBigHitPatch;
    public ConfigEntry<bool> enableBufferedInputPatch;
    public ConfigEntry<bool> enableCustomDressAnimationMod;
    public ConfigEntry<bool> enableNearestNeighborOnpuPatch;
    public ConfigEntry<bool> enableNoShadowOnpuPatch;
    public ConfigEntry<bool> enableRotatingDonChanPatch;
    public ConfigEntry<bool> enableSkipBootScreenPatch;
    public ConfigEntry<bool> enableSkipRewardPatch;
    public ConfigEntry<uint> maxBufferedInputCount;

    public static TnTrfMod Instance { get; private set; }

    public string sceneName { get; set; }

    public override void Load()
    {
        Instance = this;
        Log = base.Log;
        Log.LogInfo($"TnTRFMod has loaded!");

        enableBetterBigHitPatch = Config.Bind("General", "EnableBetterBigHitPatch", true,
            "Whether to enable better Big Hit Patch, which will treat one side hit as a big hit.");
        enableSkipBootScreenPatch = Config.Bind("General", "EnableSkipBootScreenPatch", true,
            "Whether to enable Skip Boot Screen Patch.");
        enableSkipRewardPatch = Config.Bind("General", "EnableSkipRewardPatch", true,
            "Whether to enable Skip Reward Dialog Patch.");
        enableNearestNeighborOnpuPatch = Config.Bind("General", "EnableNearestNeighborOnpuPatch", true,
            "Whether to enable Nearest Neighbor Onpu/Note Patch, this may make the notes look more pixelated.");
        enableNoShadowOnpuPatch = Config.Bind("General", "EnableNoShadowOnpuPatch", false,
            "Whether to enable No Shadow Onpu/Note Patch, this may reduce motion blur effect when notes are scrolling, but may also reduce the performance.");
        enableCustomDressAnimationMod = Config.Bind("General", "EnableCustomDressAnimationMod", false,
            "Enable a simple gui that can switch preview animation of don-chan when in dressing page.");
        enableBufferedInputPatch = Config.Bind("General", "EnableBufferedInputPatch", false,
            "Whether to enable Buffered Input Patch.");

        maxBufferedInputCount = Config.Bind("BufferedInput", "MaxBufferedInputCount", 30u,
            "The maximum count of the buffered key input per side.");

        SceneManager.sceneLoaded +=
            DelegateSupport.ConvertDelegate<UnityAction<Scene, LoadSceneMode>>(OnSceneWasLoaded);

        SetupHarmony();
    }

    private void SetupHarmony()
    {
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        var result = true;

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

    private void OnSceneWasLoaded(Scene scene, LoadSceneMode mode)
    {
        sceneName = scene.name;
        Log.LogInfo($"OnSceneWasLoaded {sceneName}");

        switch (sceneName)
        {
            case "Title":
                _ = new TextUi
                {
                    Text = $"TnTRFMod v{MyPluginInfo.PLUGIN_VERSION} (BepInEx)",
                    Position = new Vector2(32f, 32f)
                };
                break;
            case "DressUp":
            {
                if (enableCustomDressAnimationMod.Value)
                {
                    var modScene = new GameObject("DressUpModScene");
                    modScene.AddComponent<DressUpModScene>();
                }

                break;
            }
            case "Enso":
            {
                NoShadowOnpuPatch.CheckOrInitializePatch();
                BufferedNoteInputPatch.ResetCounts();

                if (enableNearestNeighborOnpuPatch.Value) NearestNeighborOnpuPatch.PatchLaneTarget();
                break;
            }
        }
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
}