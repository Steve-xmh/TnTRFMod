using TMPro;
using TnTRFMod.Patches;
using TnTRFMod.Ui.Tokkun;
using TnTRFMod.Ui.Widgets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TnTRFMod.Scenes.Enso;

public class TokkunMode
{
    private Drum drumP1;
    private Drum drumP2;

    private bool isActive = TokkunGamePatch.ShouldSkipSave;
    private bool lastPause;
    private TextUi playbackSpeedText;

    public void Start()
    {
        drumP1 = new Drum
        {
            Position = new Vector2(609f, 678f),
            Visible = false
        };
        drumP2 = new Drum
        {
            Position = new Vector2(1317f, 678f),
            Visible = false
        };
        playbackSpeedText = new TextUi
        {
            Name = "PlaybackSpeedText",
            Text = "速度: 1.0x",
            FontSize = 40,
            Alignment = TextAlignmentOptions.TopRight,
            Position = new Vector2(1465, 547),
            Visible = false
        };

        SetResumeActions();
        BufferedNoteInputPatch.OnKeyPressEvent += OnKeyPressed;
        BufferedNoteInputPatch.OnKeyPressEvent += TokkunGamePatch.OnTokkunKeyPressed;
    }

    public void Destroy()
    {
        BufferedNoteInputPatch.OnKeyPressEvent -= OnKeyPressed;
        BufferedNoteInputPatch.OnKeyPressEvent -= TokkunGamePatch.OnTokkunKeyPressed;
    }

    private void SetResumeActions()
    {
        drumP1.SetDonAction(Drum.Action.None);
        drumP1.SetLeftKatsuAction(Drum.Action.None);
        drumP1.SetRightKatsuAction(Drum.Action.None);

        drumP2.SetDonAction(Drum.Action.Pause);
        drumP2.SetLeftKatsuAction(Drum.Action.Pause);
        drumP2.SetRightKatsuAction(Drum.Action.Pause);
    }

    private void SetPausedActions()
    {
        drumP1.SetDonAction(Drum.Action.Resume);
        drumP1.SetLeftKatsuAction(Drum.Action.Rewind);
        drumP1.SetRightKatsuAction(Drum.Action.Forward);

        drumP2.SetDonAction(Drum.Action.Resume);
        drumP2.SetLeftKatsuAction(Drum.Action.SlowPlayback);
        drumP2.SetRightKatsuAction(Drum.Action.FastPlayback);
    }

    private void OnKeyPressed(Key key)
    {
        var mgr = TokkunGamePatch.EnsoGameManager;
        if (mgr == null || mgr.playerNum > 1) return;
        switch (key)
        {
            // TODO: 跟随玩家设置的按键配置
            case Key.F:
            case Key.J:
                drumP1.InvokeDon();
                break;
            case Key.D:
                drumP1.InvokeLeftKatsu();
                break;
            case Key.K:
                drumP1.InvokeRightKatsu();
                break;
            default:
            {
                if (key == TnTrfMod.Instance.p2LeftKaKey.Value)
                    drumP2.InvokeLeftKatsu();
                else if (key == TnTrfMod.Instance.p2LeftDonKey.Value)
                    drumP2.InvokeDon();
                else if (key == TnTrfMod.Instance.p2RightDonKey.Value)
                    drumP2.InvokeDon();
                else if (key == TnTrfMod.Instance.p2RightKaKey.Value)
                    drumP2.InvokeRightKatsu();
                break;
            }
            // case Key.O:
            //     var index = Random.Shared.Next(0,
            //         CommonObjects.instance.MyDataManager.MusicData.MusicInfoAccesserList.Count);
            //     var song = CommonObjects.instance.MyDataManager.MusicData.MusicInfoAccesserList._items[index];
            //     Logger.Info($"Switching to random song: {song.Id} ({song.SongNames[0]})");
            //     EnsoGameBasePatch.ChangeSongInEnsoGame(song,
            //         song.Stars[4] > 0 ? EnsoData.EnsoLevelType.Ura : EnsoData.EnsoLevelType.Mania);
            //     break;
        }
    }

    public void Update()
    {
        var mgr = TokkunGamePatch.EnsoGameManager;
        if (mgr == null || mgr.playerNum > 1) return;

        if (isActive != TokkunGamePatch.ShouldSkipSave)
        {
            isActive = TokkunGamePatch.ShouldSkipSave;
            drumP1.Visible = isActive;
            drumP2.Visible = isActive;
            playbackSpeedText.Visible = isActive;
        }

        if (TokkunGamePatch.Paused != lastPause)
        {
            lastPause = TokkunGamePatch.Paused;
            if (lastPause)
                SetPausedActions();
            else
                SetResumeActions();
        }

        playbackSpeedText.SetText("速度: {0:0.00}x", (float)TokkunGamePatch.PlaybackSpeed);

        drumP1.Update();
        drumP2.Update();
    }
}