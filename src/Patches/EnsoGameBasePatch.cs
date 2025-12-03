#if BEPINEX
using Cysharp.Threading.Tasks;
#elif MELONLOADER
using Il2CppCysharp.Threading.Tasks;
#endif
using HarmonyLib;
using TnTRFMod.Utils;
using TnTRFMod.Utils.Fumen;
using UnityEngine;
using Logger = TnTRFMod.Utils.Logger;

namespace TnTRFMod.Patches;

[HarmonyPatch]
public class EnsoGameBasePatch
{
    public static readonly PlayerState[] PlayerStates = new PlayerState[5];

    private static readonly float[] _rendaTimers = new float[5];

    // private static EnsoGameManager.State _lastState = EnsoGameManager.State.Nop;
    public static List<FumenReader.MaxScore> MaxScores { get; } = new(5);
    public static bool IsShinuchiMode { get; private set; }
    public static bool IsPlaying { get; private set; }

    [HarmonyPatch(typeof(EnsoGameManager))]
    [HarmonyPatch(nameof(EnsoGameManager.ProcPreparing))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPostfix]
    private static void EnsoGameManager_ProcPreparing_Postfix(EnsoGameManager __instance)
    {
        BufferedNoteInputPatch.ResetCounts();
        ResetCounts();
        Logger.Info("EnsoGameManager_ProcPreparing_Postfix");

        IsShinuchiMode = false;
        IsPlaying = false;

        for (var i = 0; i < __instance.settings.ensoPlayerSettings.Count; i++)
        {
            var playerSettings = __instance.settings.ensoPlayerSettings[i];
            if (playerSettings == null) break;
            IsShinuchiMode = playerSettings.shinuchi == DataConst.OptionOnOff.On;
            if (IsShinuchiMode) break;
        }

        MaxScores.Clear();
        var result = __instance.ensoParam.GetFrameResults();

        for (var i = 0; i < __instance.fumenLoader.playerData.Count; i++)
        {
            var playerData = __instance.fumenLoader.playerData[i];
            if (playerData == null) break;
            var eachPlayer = result.eachPlayer[i];
            if (eachPlayer == null) continue;
            var fumen = new FumenReader(playerData.GetFumenDataAsBytes());
            var maxScore = fumen.CalculateMaxScore();
            maxScore.noteScore = (int)eachPlayer.constShinuchiScore;
            maxScore.maxScore = (maxScore.simpleNoteAmount + maxScore.bigNoteAmount) * maxScore.noteScore;
            Logger.Info(
                $"Player {i + 1} max score: {maxScore.maxScore}, note score: {maxScore.noteScore}, simple note amount: {maxScore.simpleNoteAmount}");
            MaxScores.Add(maxScore);
            PlayerStates[i].ScoreRanks =
            [
                maxScore.maxScore * 5 / 10,
                maxScore.maxScore * 6 / 10,
                maxScore.maxScore * 7 / 10,
                maxScore.maxScore * 8 / 10,
                maxScore.maxScore * 9 / 10,
                maxScore.maxScore * 95 / 100,
                maxScore.maxScore
            ];
            PlayerStates[i].RyoJudgeRange =
                eachPlayer.GetJudgeRange(TaikoCoreTypes.OnpuTypes.Don, TaikoCoreTypes.HitResultTypes.Ryo);
            PlayerStates[i].KaJudgeRange =
                eachPlayer.GetJudgeRange(TaikoCoreTypes.OnpuTypes.Don, TaikoCoreTypes.HitResultTypes.Ka);
            PlayerStates[i].FukaJudgeRange =
                eachPlayer.GetJudgeRange(TaikoCoreTypes.OnpuTypes.Don, TaikoCoreTypes.HitResultTypes.Fuka);
        }
    }

    public static void ResetCounts()
    {
        for (var i = 0; i < PlayerStates.Length; i++)
            PlayerStates[i] = new PlayerState
            {
                LastHitTimeOffset = 0,
                AverageHitTimeOffset = 0,
                HitCount = 0,
                RyoCount = 0,
                KaCount = 0,
                FuKaCount = 0,
                RendaCount = 0,
                RyoJudgeRange = float.Epsilon,
                KaJudgeRange = float.Epsilon,
                FukaJudgeRange = float.Epsilon,
                ScoreRanks = new int[7],
                CurrentScoreRank = -1
            };
    }

    [HarmonyPatch(typeof(EnsoInput))]
    [HarmonyPatch(nameof(EnsoInput.CheckAutoRenda))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPrefix]
    private static bool EnsoInput_CheckAutoRenda_Prefix(EnsoInput __instance, ref bool __result, int player,
        int rendaFrame)
    {
        var speed = TnTrfMod.Instance.autoPlayRendaSpeed.Value;
        if (speed == 0f)
        {
            __result = false;
            return false;
        }

        var playerInfo = __instance.playerInfo[player]!;
        _rendaTimers[player] = Math.Max(0, _rendaTimers[player] - Time.deltaTime);

        __result = _rendaTimers[player] <= 0;
        if (!__result) return false;
        playerInfo.autoRendaCount++;
        _rendaTimers[player] = speed / 1000f;

        return false;
    }


    private static void OnSimpleHit(int playerNum, TaikoCoreTypes.HitResultTypes hitResult, float onpuJustTime)
    {
        PlayerStates[playerNum].RecordHit(hitResult, onpuJustTime);
    }

    private static void OnRendaHit(int playerNum)
    {
        PlayerStates[playerNum].RecordRendaHit();
    }

    private static bool ShouldSkipProcessExecMain(EnsoGameManager __instance)
    {
        return !IsPlaying || __instance.state != EnsoGameManager.State.Exec || TokkunGamePatch.Paused ||
               TnTrfMod.Instance.GetSceneName() != "Enso";
    }

    // EnsoGameManager__ProcExecMain
    // 此函数为逐帧调用，尽量避免产生过多开销
    [HarmonyPatch(typeof(EnsoGameManager))]
    [HarmonyPatch(nameof(EnsoGameManager.ProcExecMain))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPostfix]
    private static void EnsoGameManager_ProcExecMain_Postfix(EnsoGameManager __instance)
    {
        IsPlaying = __instance.ensoSound.songPlayer.IsPlaying();
        if (ShouldSkipProcessExecMain(__instance)) return;
        var results = __instance.ensoParam.GetFrameResults();

        for (var i = 0; i < results.eachPlayer.Count; i++)
        {
            var player = results.eachPlayer[i];
            if (player == null) continue;
            var playerState = PlayerStates[i];
            var nextScoreRank = playerState.CurrentScoreRank + 1;
            if (nextScoreRank >= playerState.ScoreRanks.Length) continue;
            if (player.score >= playerState.ScoreRanks[nextScoreRank])
            {
                PlayerStates[i].CurrentScoreRank = nextScoreRank;
                Logger.Info($"Player {i + 1} has reached score rank {nextScoreRank}");
            }
        }

        // Logger.Info($"donRange {ryoRange}ms kaRange {kaRange}ms fukaRange {fukaRange}ms hitResultInfoMax {results.hitResultInfoMax} hitResultInfoNum {results.hitResultInfoNum}");
        // Logger.Info($"results.firstOnpu.state {results.firstOnpu.state}");
        for (var i = 0; i < results.hitResultInfoNum; i++)
        {
            var hit = results.hitResultInfo[i];
            if (hit == null) continue;
            var hitResult = (TaikoCoreTypes.HitResultTypes)hit.hitResult;
            var onpuType = (TaikoCoreTypes.OnpuTypes)hit.onpuType;
            if (hitResult == TaikoCoreTypes.HitResultTypes.None) continue;

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (onpuType)
            {
                case TaikoCoreTypes.OnpuTypes.Don:
                case TaikoCoreTypes.OnpuTypes.Do:
                case TaikoCoreTypes.OnpuTypes.Ko:
                case TaikoCoreTypes.OnpuTypes.Katsu:
                case TaikoCoreTypes.OnpuTypes.Ka:
                case TaikoCoreTypes.OnpuTypes.WDon:
                case TaikoCoreTypes.OnpuTypes.DaiDon:
                case TaikoCoreTypes.OnpuTypes.DaiKatsu:
                    // 音符判定调整： __instance.settings.noteDelay
                    // 太鼓控制器判定调整： __instance.settings.tatakonDelay
                    var onpuJustTime = hit.onpu.justTime - __instance.ensoParam.TotalTime -
                                       __instance.settings.noteDelay * 5;
                    // Console.Out.WriteLine($"Onpu Type: {hitResult} noteDelay: {__instance.settings.noteDelay} tatakonDelay: {__instance.settings.tatakonDelay}");
                    OnSimpleHit(hit.player, hitResult, onpuJustTime);
                    __instance.taikoCorePlayer.GetRyo((TaikoCoreTypes.BranchTypes)hit.onpu.branchType);
                    break;
                case TaikoCoreTypes.OnpuTypes.GekiRenda:
                case TaikoCoreTypes.OnpuTypes.DaiRenda:
                case TaikoCoreTypes.OnpuTypes.Renda:
                {
                    switch (hitResult)
                    {
                        case TaikoCoreTypes.HitResultTypes.Ryo:
                        case TaikoCoreTypes.HitResultTypes.Ka:
                            OnRendaHit(hit.player);
                            break;
                    }

                    break;
                }
            }
        }
    }

    // [HarmonyPatch(typeof(EnsoGameManager))]
    // [HarmonyPatch(nameof(EnsoGameManager.Update))]
    // [HarmonyPatch(MethodType.Normal)]
    // [HarmonyPostfix]
    // private static void EnsoGameManager_Update_Postfix(EnsoGameManager __instance)
    // {
    //     if (_lastState != __instance.state)
    //     {
    //         Logger.Info($"EnsoGameManager state changed: {_lastState} -> {__instance.state}");
    //         _lastState = __instance.state;
    //         if (__instance.state == EnsoGameManager.State.Error) Debugger.Break();
    //     }
    //     else if (__instance.state != EnsoGameManager.State.Exec)
    //     {
    //         Logger.Info($"EnsoGameManager state: {__instance.state}");
    //     }
    // }

    /// <summary>
    ///     在演奏模式下尝试切换乐曲
    /// </summary>
    /// <param name="song"></param>
    /// <param name="difficulty"></param>
    public static void ChangeSongInEnsoGame(MusicDataInterface.MusicInfoAccesser song,
        EnsoData.EnsoLevelType difficulty)
    {
        UTask.RunOnIl2CppBlocking(() =>
        {
            if (CommonObjects.instance.MySceneManager.CurrentSceneName != "Enso")
                throw new InvalidOperationException("Cannot change song when not in Enso scene");
            var ensoGameManagerGameObject = GameObject.Find("EnsoGameManager");
            if (ensoGameManagerGameObject == null)
                throw new InvalidOperationException("EnsoGameManager not found in scene");
            var ensoGameManager = ensoGameManagerGameObject.GetComponent<EnsoGameManager>();
            if (ensoGameManager == null)
                throw new InvalidOperationException(
                    "EnsoGameManager component not found in EnsoGameManager game object");

            // TODO: 需要重置难度图标，小咚角色状态
            var settings = DecideEnsoSettingsForSong(ref song, difficulty);
            ensoGameManager.settings = settings;
            ensoGameManager.ensoSound.StopSong();
            ensoGameManager.ensoSound.KeyOffAll(true);
            ensoGameManager.ensoSound.songPlayer = new CriPlayer(true);
            ensoGameManager.ensoSound.songPlayer.CueSheetName = song.SongFileName;
            if (song.InPackage == MusicDataInterface.InPackageType.HasSongAndFumen)
                TnTrfMod.Instance.StartCoroutine(ensoGameManager.ensoSound.songPlayer.LoadAsync());
            else
                ensoGameManager.ensoSound.songPlayer.LoadLocalStorageData(song.UniqueId).Forget();
            ensoGameManager.ensoSound.loadState = EnsoSound.LoadState.Song;
            ensoGameManager.fumenLoader.Dispose();
            ensoGameManager.fumenLoader.Awake();
            ensoGameManager.taikoCorePlayer.Initialize(ensoGameManager.ensoInput, ensoGameManager.ensoSound);

            var songInfoGO = GameObject.Find("SongInfo");
            var songInfoPlayer = songInfoGO.GetComponent<SongInfoPlayer>();
            songInfoPlayer.m_songId = song.Id;
            songInfoPlayer.m_SongName = songInfoPlayer.GetSongName(song.Id);
            songInfoPlayer.m_Genre = (EnsoData.SongGenre)song.GenreNo;
            songInfoPlayer.m_bExecute = true;

            ensoGameManager.graphicManager.state = EnsoGraphicManager.State.Preparing;
            ensoGameManager.isLoadingOne = false;
            ensoGameManager.totalTime = 0;
            ensoGameManager.adjustTime = 0;
            ensoGameManager.subTime = 0;
            ensoGameManager.RestartPlay();
            ensoGameManager.fumenLoader.state = FumenLoader.State.LoadStart;
            ensoGameManager.state = EnsoGameManager.State.Loading;
        });
    }

    private static EnsoData.Settings DecideEnsoSettingsForSong(ref MusicDataInterface.MusicInfoAccesser song,
        EnsoData.EnsoLevelType difficulty)
    {
        var ensoData = CommonObjects.instance.MyDataManager.EnsoData;
        var settings = ensoData.ensoSettings;

        Logger.Info($"Starting Music {song.Id} ({song.SongFileName})");
        settings.musicuid = "";
        settings.SongFileName = "";
        settings.musicUniqueId = song.UniqueId;
        settings.genre = (EnsoData.SongGenre)song.GenreNo;
        settings.ensoType = EnsoData.EnsoType.Normal;
        settings.playerNum = 1;
        var firstPlayerSetting = settings.ensoPlayerSettings[0]!;
        firstPlayerSetting.courseType = difficulty;
        settings.ensoPlayerSettings[0] = firstPlayerSetting;

        ensoData.ensoSettings = settings;

        CommonObjects.instance.MySoundManager.SetEnsoVolume(ref settings);
        ensoData.DecideSetting();
        return settings;
    }

    /// <summary>
    ///     尝试强制开启某个乐曲的单人演奏模式
    /// </summary>
    /// <param name="song"></param>
    /// <param name="difficulty"></param>
    public static void StartEnsoGame(ref MusicDataInterface.MusicInfoAccesser song, EnsoData.EnsoLevelType difficulty)
    {
        DecideEnsoSettingsForSong(ref song, difficulty);
        CommonObjects.instance.MySceneManager.ChangeSceneAsync("Enso").Forget();
    }

    [HarmonyPatch(typeof(EnsoGameManager))]
    [HarmonyPatch(nameof(EnsoGameManager.SetResults))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPrefix]
    private static bool EnsoGameManager_SetResults_Prefix(EnsoGameManager __instance)
    {
        Logger.Info("EnsoGameManager_SetResults_Prefix");
        if (FumenPostProcessingPatch.HasAnyPostProcessing)
            return false;
        var mainPlayer = __instance.settings.ensoPlayerSettings[0];
        if (mainPlayer == null) return true;
        if (mainPlayer.special == DataConst.SpecialTypes.Auto) return true;

        Logger.Info("Saving custom hit counts");
        // var songUniqueId = __instance.settings.musicUniqueId;
        // CustomSongSaveDataPatch.ApplyModSaveData(songUniqueId, data =>
        // {
        //     data.scoreRanks ??= [-1, -1, -1, -1, -1];
        //     var curRank = data.scoreRanks[(int)mainPlayer.courseType];
        //     data.scoreRanks[(int)mainPlayer.courseType] = Math.Max(curRank, PlayerStates[0].CurrentScoreRank);
        //     return data;
        // });
        return true;
    }

    public struct PlayerState
    {
        public float LastHitTimeOffset;
        public float AverageHitTimeOffset;
        public int HitCount;
        public int RyoCount;
        public int KaCount;
        public int FuKaCount;
        public int RendaCount;
        public float RyoJudgeRange;
        public float KaJudgeRange;
        public float FukaJudgeRange;
        public int CurrentScoreRank;
        public int[] ScoreRanks;

        public void RecordHit(TaikoCoreTypes.HitResultTypes hitResult, float onpuJustTime)
        {
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (hitResult)
            {
                case TaikoCoreTypes.HitResultTypes.Ryo:
                    RyoCount++;
                    HitCount++;
                    LastHitTimeOffset = onpuJustTime;
                    AverageHitTimeOffset = (onpuJustTime + AverageHitTimeOffset * (HitCount - 1)) / HitCount;
                    break;
                case TaikoCoreTypes.HitResultTypes.Ka:
                    KaCount++;
                    HitCount++;
                    LastHitTimeOffset = onpuJustTime;
                    AverageHitTimeOffset = (onpuJustTime + AverageHitTimeOffset * (HitCount - 1)) / HitCount;
                    break;
                case TaikoCoreTypes.HitResultTypes.Drop:
                    FuKaCount++;
                    break;
                case TaikoCoreTypes.HitResultTypes.Fuka:
                    FuKaCount++;
                    LastHitTimeOffset = onpuJustTime;
                    break;
            }
        }

        public void RecordRendaHit()
        {
            RendaCount++;
        }
    }
}