using System.Collections;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;
using TnTRFMod.Config;
using TnTRFMod.Patches;
using TnTRFMod.Scenes;
using TnTRFMod.Ui;
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
    public const string MOD_VERSION = "0.6.0";
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

    public static readonly string Dir = Path.Combine(Application.dataPath, "../TnTRFMod");

#if BEPINEX
    internal Updater _updater;
#endif
    public ConfigEntry<bool> enableAutoDownloadSubscriptionSongs;

    public ConfigEntry<bool> enableBetterBigHitPatch;
    public ConfigEntry<bool> enableBufferedInputPatch;
    public ConfigEntry<bool> enableCustomDressAnimationMod;
    public ConfigEntry<bool> enableMinimumLatencyAudioClient;
    public ConfigEntry<bool> enableOpenInviteFriendDialogButton;
    public ConfigEntry<bool> enableHitStatsPanelPatch;
    public ConfigEntry<bool> enableScoreRankIcon;
    public ConfigEntry<bool> enableOnpuTextRail;
    public ConfigEntry<bool> enableNearestNeighborOnpuPatch;
    public ConfigEntry<bool> enableNoShadowOnpuPatch;
    public ConfigEntry<bool> enableSkipBootScreenPatch;
    public ConfigEntry<bool> enableSkipRewardPatch;
    public ConfigEntry<bool> enableLouderSongPatch;
    public ConfigEntry<uint> maxBufferedInputCount;

    // 直播点歌功能
    public ConfigEntry<bool> enableBilibiliLiveStreamSongRequest;
    public ConfigEntry<uint> bilibiliLiveStreamSongRoomId;
    public ConfigEntry<string> bilibiliLiveStreamSongToken;

    public static TnTrfMod Instance { get; internal set; }

    private string sceneName { get; set; }

    // "H:\SteamLibrary\steamapps\common\Taiko no Tatsujin Rhythm Festival\Taiko no Tatsujin Rhythm Festival_Data\Plugins\x86_64\LibTaiko.dll"
    [DllImport("Taiko no Tatsujin Rhythm Festival_Data/Plugins/x86_64/LibTaiko.dll", EntryPoint = "SetDebugLogFunc",
        CallingConvention = CallingConvention.StdCall)]
    private static extern void SetLibTaikoDebugLogFunc(OnLibTaikoLog func);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void OnLibTaikoLog(IntPtr msgBuffer);

    public void Load(HarmonyInstance harmony)
    {
        _harmony = harmony;
        Logger.Info("TnTRFMod has loaded!");
        I18n.Load();
        SetLibTaikoDebugLogFunc(buffer =>
        {
            var msg = Marshal.PtrToStringAnsi(buffer);
            Console.Out.Write(msg);
        });

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
        enableAutoDownloadSubscriptionSongs = ConfigEntry.Register("General", "EnableAutoDownloadSubscriptionSongs",
            "Enable auto download subscription songs. (NOT FULLY TESTED)", true);
        enableOnpuTextRail = ConfigEntry.Register("General", "EnableOnpuTextRail",
            "Draw an nijiiro-like note text rail background.", true);
        // 默认禁用的功能
        enableNearestNeighborOnpuPatch = ConfigEntry.Register("General", "EnableNearestNeighborOnpuPatch",
            "Whether to enable Nearest Neighbor Onpu/Note Patch, this may make the notes look more pixelated.", false);
        enableNoShadowOnpuPatch = ConfigEntry.Register("General", "EnableNoShadowOnpuPatch",
            "Whether to enable No Shadow Onpu/Note Patch, this may reduce motion blur effect when notes are scrolling, but may also reduce the performance.",
            false);
        enableCustomDressAnimationMod = ConfigEntry.Register("General", "EnableCustomDressAnimationMod",
            "Enable a simple gui that can switch preview animation of don-chan when in dressing page.", false);
        enableOpenInviteFriendDialogButton = ConfigEntry.Register("General", "EnableOpenInviteFriendDialogButton",
            "Enable open invite friend dialog button when in online friend room lobby.", false);
        enableHitStatsPanelPatch = ConfigEntry.Register("General", "EnableHitStatsPanelPatch",
            "Enable hit stats panel during music game.",
            false);
        enableLouderSongPatch = ConfigEntry.Register("General", "EnableLouderSongPatch",
            "Allow to play little louder song",
            false);
        enableScoreRankIcon = ConfigEntry.Register("General", "EnableScoreRankIcon",
            "Enable score rank icon during music game.",
            false);
        // 直播点歌功能
        enableBilibiliLiveStreamSongRequest = ConfigEntry.Register("BilibiliLiveStreamSongRequest", "Enable",
            "Enable Bilibili Live Stream Song Request", false);
        bilibiliLiveStreamSongRoomId = ConfigEntry.Register("BilibiliLiveStreamSongRequest", "RoomId",
            "Bilibili Live Stream Room Id", 0u);
        bilibiliLiveStreamSongToken = ConfigEntry.Register("BilibiliLiveStreamSongRequest", "Token",
            "Bilibili Live Stream Token. Commonly as a Cookie called \"SESSDATA\". If you don't provide this, you won't be able to get accurate sender info.",
            "");

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
        result &= PatchClass<ReopenInviteDialogPatch>(enableOpenInviteFriendDialogButton);
        result &= PatchClass<ForcePlayMusicPatch>(enableLouderSongPatch);
        result &= PatchClass<EnsoGameBasePatch>();
        result &= PatchClass<LibTaikoPatches>();

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

    public void OnUpdate()
    {
        if (sceneName != null && _scenes[sceneName] is IScene customScene) customScene.Update();
    }

    private void OnSceneWasLoaded(Scene scene, LoadSceneMode mode)
    {
        sceneName = scene.name;
        Logger.Info($"OnSceneWasLoaded {sceneName}");

        Common.Init();
        Common.InitLocal();
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
        s.Init();
        if (_scenes[s.SceneName] is IScene customScene)
        {
            Logger.Error($"Scene already registered by {customScene.GetType().Name}, skipping...");
            return;
        }

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
        RegisterScene<EnsoTestScene>();
        RegisterScene<BootScene>();
        RegisterScene<EnsoNetworkScene>();
        RegisterScene<OnlineModJoinLobbyScene>();
        RegisterScene<SongSelectScene>();
    }

    internal class Updater : MonoBehaviour
    {
        private void Update()
        {
            Instance.OnUpdate();
        }
    }
}