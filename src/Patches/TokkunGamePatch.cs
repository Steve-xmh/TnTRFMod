using System.Text;
using HarmonyLib;
using TnTRFMod.Ui;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = TnTRFMod.Utils.Logger;

#if BEPINEX
using SoundLabelClass = SoundLabel.SoundLabel;

#elif MELONLOADER
using SoundLabelClass = Il2CppSoundLabel.SoundLabel;
#endif

namespace TnTRFMod.Patches;

[HarmonyPatch]
public class TokkunGamePatch
{
    private static LoggingScreenUi.LogHandle? pauseLocker;
    private static int seekSyousetsuIndex = -1;
    private static int lastResumeSyousetsuIndex = -1;
    private static double targetSeekTime;
    private static double curSeekTime;
    private static bool waitForPreparingSong;
    private static CriAtomExStandardVoicePool? voicePool;

    private static CriAtomExPlayer? playbackSound;
    private static CriAtomExPlayer? speedFastSound;
    private static CriAtomExPlayer? speedSlowSound;

    private static CriAtomExAcb? seAcb;

    private static readonly double[] PlaybackSpeedList =
    [
        0.4, 0.5, 0.6, 0.75, 0.8, 0.9, 1.0, 1.1, 1.2, 1.25, 1.3, 1.4, 1.5, 1.6, 1.7, 1.75, 1.8, 1.9, 2.0, 2.25, 2.5,
        2.75, 3.0, 3.5, 4.0
    ];

    private static int playbackSpeedIndex = 6; // 默认1.0倍速

    private static EnsoGameManager? _ensoGameManager;
    public static bool ShouldSkipSave { get; private set; }
    public static double PlaybackSpeed => PlaybackSpeedList[playbackSpeedIndex];

    public static bool Paused { get; private set; }

    public static EnsoGameManager? EnsoGameManager
    {
        get
        {
            if (_ensoGameManager == null) return null;
            if (_ensoGameManager.WasCollected) return null;
            return _ensoGameManager;
        }
    }

    [HarmonyPatch(typeof(EnsoGameManager))]
    [HarmonyPatch(nameof(EnsoGameManager.ProcPreparing))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPostfix]
    private static void EnsoGameManager_ProcPreparing_Postfix(EnsoGameManager __instance)
    {
        if (TnTrfMod.Instance.GetSceneName() != "Enso") return;
        if (__instance.state != EnsoGameManager.State.ToExec) return;
        _ensoGameManager = __instance;

        Paused = false;
        seekSyousetsuIndex = -1;
        playbackSpeedIndex = 6;
        targetSeekTime = 0;
        waitForPreparingSong = false;
        pauseLocker?.Dispose();
        pauseLocker = null;
        ShouldSkipSave = false;
        EnsoData.FumenTimeScale = 1.0f;
        EnsoData.SongTimeScale = 1.0f;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var sePath = Path.Join(TnTrfMod.Dir, "tokkun_se/tokkun_se.acb");
        var seData = File.ReadAllBytes(sePath);
        seAcb = CriAtomExAcb.LoadAcbData(seData, null, null);
        LoadSE(ref playbackSound, "tokkun_playback_changed");
        LoadSE(ref speedFastSound, "tokkun_speed_fast");
        LoadSE(ref speedSlowSound, "tokkun_speed_slow");
    }

    private static bool ShouldBypassEnsoEndTypes(ref EnsoGameManager __instance)
    {
        switch (__instance.ensoParam.EnsoEndType)
        {
            case EnsoPlayingParameter.EnsoEndTypes.PauseMenuRetry:
            case EnsoPlayingParameter.EnsoEndTypes.PauseMenuEnd:
            case EnsoPlayingParameter.EnsoEndTypes.PauseMenuEndMission:
            case EnsoPlayingParameter.EnsoEndTypes.PauseMenuEndEntrance:
            case EnsoPlayingParameter.EnsoEndTypes.PauseMenuEndMode:
            case EnsoPlayingParameter.EnsoEndTypes.PauseMenuEndTitle:
                return true;
            case EnsoPlayingParameter.EnsoEndTypes.None:
            case EnsoPlayingParameter.EnsoEndTypes.Normal:
            case EnsoPlayingParameter.EnsoEndTypes.OptionPerfect:
            case EnsoPlayingParameter.EnsoEndTypes.OptionTraining:
            case EnsoPlayingParameter.EnsoEndTypes.AdjustEnd:
            case EnsoPlayingParameter.EnsoEndTypes.Retire:
            case EnsoPlayingParameter.EnsoEndTypes.NetworkError:
            case EnsoPlayingParameter.EnsoEndTypes.PauseMenuSkip:
            default:
                return false;
        }
    }

    [HarmonyPatch(typeof(EnsoGameManager))]
    [HarmonyPatch(nameof(EnsoGameManager.ProcExecMain))]
    [HarmonyPrefix]
    private static bool EnsoGameManager_ProcExecMain_Prefix(ref EnsoGameManager __instance)
    {
        _ensoGameManager = __instance;
        if (__instance.playerNum > 1) return true; // 仅单人模式有效
        if (TnTrfMod.Instance.GetSceneName() != "Enso") return true;
        if (ShouldBypassEnsoEndTypes(ref __instance)) return true;
        if (waitForPreparingSong)
        {
            if (!__instance.ensoSound.IsPreparSongFinished()) return false; // 等待歌曲准备完成
            waitForPreparingSong = false;
            __instance.ensoSound.PlaySong();

            __instance.totalTime = (__instance.ensoSound.GetSongPosition(true) + EnsoData.TimeAdjustBaseDelay) /
                                   PlaybackSpeed;
            __instance.adjustTime = playbackSpeedIndex switch
            {
                < 6 => TnTrfMod.Instance.tokkunGameSlowTimeOffset.Value,
                > 6 => TnTrfMod.Instance.tokkunGameFastTimeOffset.Value,
                _ => __instance.adjustTime
            };

            return false; // 等待歌曲准备完成
        }

        if (Paused)
        {
            curSeekTime += (targetSeekTime - curSeekTime) * Time.deltaTime * 10f;
            __instance.taikoCorePlayer.ResetToRetry();
            __instance.taikoCorePlayer.Update((float)curSeekTime);
            __instance.graphicManager.Update();
        }

        return !Paused;
    }

    [HarmonyPatch(typeof(EnsoGameManager))]
    [HarmonyPatch(nameof(EnsoGameManager.UpdateSongAdjustParamsNew))]
    [HarmonyPrefix]
    public static bool EnsoGameManager_UpdateSongAdjustParamsNew_Prefix(ref EnsoGameManager __instance)
    {
        // 在这里写入函数主体
        return false; // 取消运行原本的函数体
    }

    public static void OnTokkunKeyPressed(Key key)
    {
        var mgr = EnsoGameManager;
        if (mgr == null) return;
        if (mgr.playerNum > 1) return; // 仅单人模式有效
        if (Paused)
        {
            switch (key)
            {
                case Key.D:
                    seekSyousetsuIndex = Math.Max(0, seekSyousetsuIndex - 1);
                    SeekToSyousetsu(ref mgr);
                    UpdatePauseText();
                    PlayKatsuSound(ref mgr);
                    break;
                case Key.K:
                    seekSyousetsuIndex += 1;
                    SeekToSyousetsu(ref mgr);
                    UpdatePauseText();
                    PlayKatsuSound(ref mgr);
                    break;
                case Key.Z:
                    playbackSpeedIndex = Math.Max(0, playbackSpeedIndex - 1);
                    UpdatePauseText();
                    PlaySpeedSlowSound(ref mgr);
                    break;
                case Key.V:
                    playbackSpeedIndex = Math.Min(PlaybackSpeedList.Length - 1, playbackSpeedIndex + 1);
                    UpdatePauseText();
                    PlaySpeedFastSound(ref mgr);
                    break;
                case Key.F:
                case Key.J:
                    Paused = false;
                    Resume(ref mgr);
                    PlayPlaybackSound(ref mgr);
                    break;
                default:
                {
                    if (key == TnTrfMod.Instance.p2LeftKaKey.Value)
                    {
                        playbackSpeedIndex = Math.Max(0, playbackSpeedIndex - 1);
                        UpdatePauseText();
                        PlaySpeedSlowSound(ref mgr);
                    }
                    else if (key == TnTrfMod.Instance.p2RightKaKey.Value)
                    {
                        playbackSpeedIndex = Math.Min(PlaybackSpeedList.Length - 1, playbackSpeedIndex + 1);
                        UpdatePauseText();
                        PlaySpeedFastSound(ref mgr);
                    }
                    else if (key == TnTrfMod.Instance.p2LeftDonKey.Value ||
                             key == TnTrfMod.Instance.p2RightDonKey.Value)
                    {
                        Paused = false;
                        Resume(ref mgr);
                        PlayPlaybackSound(ref mgr);
                    }

                    break;
                }
            }
        }
        else if (key == TnTrfMod.Instance.p2LeftKaKey.Value ||
                 key == TnTrfMod.Instance.p2LeftDonKey.Value ||
                 key == TnTrfMod.Instance.p2RightDonKey.Value ||
                 key == TnTrfMod.Instance.p2RightKaKey.Value)
        {
            Paused = true;
            Pause(ref mgr);
            UpdatePauseText();
            PlayPlaybackSound(ref mgr);
        }
    }

    [HarmonyPatch(typeof(EnsoGameManager))]
    [HarmonyPatch(nameof(EnsoGameManager.ProcExecMain))]
    [HarmonyPostfix]
    private static void EnsoGameManager_ProcExecMain_Postfix(ref EnsoGameManager __instance)
    {
        _ensoGameManager = __instance;
        if (!ShouldSkipSave) return;
        if (!__instance.ensoParam.IsFixResult) return;
        if (TnTrfMod.Instance.GetSceneName() != "Enso") return;
        if (ShouldBypassEnsoEndTypes(ref __instance)) return;

        Logger.Info("EnsoGameManager.ExecToNext: Skipping save.");
        Pause(ref __instance, true);
        Paused = true;

        if (TnTrfMod.Instance.tokkunGameOnSongEndBehaviour.Value == "PauseAtLastMeasure")
            seekSyousetsuIndex = Math.Max(0,
                __instance.taikoCorePlayer.GetNumberOfSyousetsuByTime(0, float.MaxValue, true) - 1);
        else if (TnTrfMod.Instance.tokkunGameOnSongEndBehaviour.Value == "ToLastResumePosition")
            seekSyousetsuIndex = lastResumeSyousetsuIndex == -1 ? 0 : lastResumeSyousetsuIndex;
        else
            seekSyousetsuIndex = 0;

        SeekToSyousetsu(ref __instance);
        UpdatePauseText();

        __instance.state = EnsoGameManager.State.Exec;
        __instance.ensoParam.EnsoEndType = EnsoPlayingParameter.EnsoEndTypes.None;
        __instance.ensoParam.isToResult = false;
        __instance.ensoParam.IsFixResult = false;
        PlayKatsuSound(ref __instance);
    }

    private static void PlayKatsuSound(ref EnsoGameManager __instance)
    {
        if (Paused) __instance.ensoSound.SoundManager.PlayCommonSe(SoundLabelClass.Common.katsu);
    }

    private static void LoadSE(ref CriAtomExPlayer? player, string cueName = "")
    {
        if (seAcb == null) return;
        player = new CriAtomExPlayer();
        player.SetCue(seAcb, cueName);
        player.Prepare();
    }

    private static void PlayPlaybackSound(ref EnsoGameManager __instance)
    {
        if (playbackSound == null)
        {
            __instance.ensoSound.SoundManager.PlayCommonSe(SoundLabelClass.Common.katsu);
        }
        else
        {
            playbackSound.Stop();
            playbackSound.Start();
        }
    }

    private static void PlaySpeedFastSound(ref EnsoGameManager __instance)
    {
        if (speedFastSound == null)
        {
            __instance.ensoSound.SoundManager.PlayCommonSe(SoundLabelClass.Common.katsu);
        }
        else
        {
            speedFastSound.Stop();
            speedFastSound.Start();
        }
    }

    private static void PlaySpeedSlowSound(ref EnsoGameManager __instance)
    {
        if (speedSlowSound == null)
        {
            __instance.ensoSound.SoundManager.PlayCommonSe(SoundLabelClass.Common.katsu);
        }
        else
        {
            speedSlowSound.Stop();
            speedSlowSound.Start();
        }
    }

    private static void UpdatePauseText()
    {
        // var text = $"演奏已暂停\n" +
        //            $"当前小节: {seekSyousetsuIndex + 1}\n" +
        //            $"回放速度：{(int)Math.Round(PlaybackSpeed * 100)}%\n" +
        //            $"当前位置：{targetSeekTime / 1000f:F2} 秒\n" +
        //            $"按下 X 键或 C 键继续\n" +
        //            $"按下 D / K 跳转到上 / 下一小节\n" +
        //            $"按下 Z / V 调节播放速度";
        // pauseLocker ??= LoggingScreenUi.New(text);
        // pauseLocker.Text = text;
    }

    private static void Pause(ref EnsoGameManager __instance, bool isSongEnd = false)
    {
        __instance.ensoSound.StopSong();
        ShouldSkipSave = true;

        curSeekTime = __instance.totalTime * PlaybackSpeed + __instance.tempAdjustTime;
        if (!isSongEnd)
        {
            Logger.Info("TnTrfMod.Instance.tokkunGameOnPauseBehaviour: " +
                        TnTrfMod.Instance.tokkunGameOnPauseBehaviour.Value);
            if (TnTrfMod.Instance.tokkunGameOnPauseBehaviour.Value == "ToLastPausePosition")
                seekSyousetsuIndex = lastResumeSyousetsuIndex == -1
                    ? __instance.taikoCorePlayer.GetNumberOfSyousetsuByTime(0, (float)curSeekTime, false)
                    : lastResumeSyousetsuIndex;
            else
                seekSyousetsuIndex =
                    __instance.taikoCorePlayer.GetNumberOfSyousetsuByTime(0, (float)curSeekTime, false);
        }

        Logger.Info("Current curSeekTime: " + curSeekTime);
        SeekToSyousetsu(ref __instance);
        PrintTargetSeekTime(ref __instance);
    }

    private static void PrintTargetSeekTime(ref EnsoGameManager __instance)
    {
        var targetTime = __instance.taikoCorePlayer.GetSyousetsuJustTime(0, seekSyousetsuIndex);
        Logger.Info($"TokunGamePatch: targetSeekTime: {targetTime}, seekSyousetsuIndex: {seekSyousetsuIndex}");
    }

    private static double GetSyousetsuJustTime(ref EnsoGameManager __instance, int syousetsuIndex)
    {
        var playSyousetsuIndex = Math.Max(0, syousetsuIndex - 1);
        return __instance.taikoCorePlayer.GetSyousetsuJustTime(0, playSyousetsuIndex) +
               __instance.tempAdjustTime;
    }

    private static double GetResumeJustTime(ref EnsoGameManager __instance)
    {
        lastResumeSyousetsuIndex = seekSyousetsuIndex;
        return GetSyousetsuJustTime(ref __instance, seekSyousetsuIndex);
    }

    private static void Resume(ref EnsoGameManager __instance)
    {
        // 单位是毫秒
        __instance.ensoSound.StopSong();
        var resumeJustTime = GetResumeJustTime(ref __instance);
        if (playbackSpeedIndex == 6)
        {
            __instance.ensoSound.songPlayer.Player.SetVoicePoolIdentifier(0);
            __instance.ensoSound.songPlayer.Player.SetDspTimeStretchRatio(1.0f);
            __instance.totalTime = resumeJustTime;
        }
        else
        {
            // 1, 2, 48000 * 2, false, 12125U
            if (voicePool == null)
                voicePool = new CriAtomExStandardVoicePool(new CriAtomExStandardVoicePool.Config
                {
                    identifier = 12125U,
                    numVoices = 1,
                    isStreamingOnly = false,
                    minChannels = 2,
                    playerConfig = new CriAtomExVoicePool.PlayerConfig
                    {
                        decodeLatency = 0,
                        maxChannels = 2,
                        maxSamplingRate = 48000 * 2,
                        streamingFlag = false,
                        soundRendererType = (int)CriAtomEx.SoundRendererType.Native
                    }
                });
            voicePool.AttachDspTimeStretch();
            __instance.ensoSound.songPlayer.Player.SetVoicePoolIdentifier(voicePool.identifier);
            __instance.ensoSound.songPlayer.Player.SetDspTimeStretchRatio((float)(1.0 / PlaybackSpeed));
            __instance.ensoSound.songPlayer.Player.SetDspParameter(
                (int)CriAtomExPlayer.TimeStretchParameterId.FrameTime, 10);
            __instance.ensoSound.songPlayer.Player.SetDspParameter((int)CriAtomExPlayer.TimeStretchParameterId.Quality,
                10);
            __instance.totalTime = resumeJustTime / PlaybackSpeed;
        }

        __instance.ensoSound.songPlayer.Player.Update(__instance.ensoSound.songPlayer.Playback);
        __instance.ensoSound.PrepareSong((long)resumeJustTime);

        EnsoData.FumenTimeScale = PlaybackSpeed;
        EnsoData.SongTimeScale = PlaybackSpeed;
        waitForPreparingSong = true;
        pauseLocker?.Dispose();
        pauseLocker = null;
        EnsoGameBasePatch.ResetCounts();
        BufferedNoteInputPatch.ResetCounts();
        __instance.taikoCorePlayer.ResetToRetry();
        if (targetSeekTime > 0f)
            __instance.taikoCorePlayer.TerminateOnpu(0f, (float)targetSeekTime);
    }

    private static void SeekToSyousetsu(ref EnsoGameManager __instance)
    {
        __instance.taikoCorePlayer.ResetToRetry();
        targetSeekTime = __instance.taikoCorePlayer.GetSyousetsuJustTime(0, seekSyousetsuIndex) +
                         __instance.tempAdjustTime;
    }
}