using Il2CppInterop.Runtime;
using TnTRFMod.Scenes.Enso;
using TnTRFMod.Ui.Widgets;
using TnTRFMod.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
#if BEPINEX
using Scripts.OutGame.SongSelect;

#elif MELONLOADER
using Il2CppScripts.OutGame.SongSelect;
#endif

namespace TnTRFMod.Scenes;

public class SongSelectScene : IScene
{
    private static readonly ControllerManager mgr = TaikoSingletonMonoBehaviour<ControllerManager>.Instance;
    private SongSelectSceneUiController? _cached;
    private TextUi liveStreamSongRequestStatus;

    private float repeatCounter;

    private Il2CppSystem.Action<char>? textInputDelegate;
    public string SceneName => "SongSelect";

    public void Start()
    {
        if (TnTrfMod.Instance.enableTatakonKeyboardSongSelect.Value)
        {
            textInputDelegate =
                DelegateSupport.ConvertDelegate<Il2CppSystem.Action<char>>(OnTextInput);
            Keyboard.current.add_onTextInput(textInputDelegate);
        }

        _cached = null;
        if (TnTrfMod.Instance.enableBilibiliLiveStreamSongRequest.Value)
        {
            liveStreamSongRequestStatus = new TextUi
            {
                FontSize = 20,
                Position = new Vector2(1500f, 863f)
            };
            UpdateLiveStreamSongRequestStatus();
        }

        // var searchTextInput = new TextFieldUi();
        // searchTextInput.Name = "SearchTextInput";
    }

    public void Destroy()
    {
        if (textInputDelegate != null)
            Keyboard.current.remove_onTextInput(textInputDelegate);
    }

    public void Update()
    {
        if (!TnTrfMod.Instance.enableBilibiliLiveStreamSongRequest.Value) return;

        repeatCounter = Math.Max(0, repeatCounter - Time.deltaTime);

        if (Keyboard.current[Key.O].wasPressedThisFrame)
            UpdatePlaylistToQueuedSongList();
        if (Keyboard.current[Key.L].wasPressedThisFrame)
        {
            var uiController = GetUiSongScroller();
            var selectedSongId = uiController.songScroller.SelectedItem.Value?.Value?.Id;
            if (selectedSongId != null)
            {
                LiveStreamSongSelectPanel.QueuedSongList.RemoveAt(
                    LiveStreamSongSelectPanel.QueuedSongList.FindIndex(x => x.SongInfo.Id == selectedSongId));
                UpdatePlaylistToQueuedSongList();
            }
        }

        UpdateLiveStreamSongRequestStatus();
    }

    private void OnTextInput(char character)
    {
        var donLKey = mgr.keyConfig[(int)ControllerManager.Taiko.DonL];
        var donRKey = mgr.keyConfig[(int)ControllerManager.Taiko.DonR];
        var katsuLKey = mgr.keyConfig[(int)ControllerManager.Taiko.KatsuL];
        var katsuRKey = mgr.keyConfig[(int)ControllerManager.Taiko.KatsuR];
        var charCode = (short)KeyConversion.CharToKey(character);

        if (charCode == katsuLKey)
        {
            var uiController = GetUiSongScroller();
            switch (uiController.focus)
            {
                case Focuses.Filters:
                    uiController.filterScroller.OnDirectionInput(ControllerManager.Dir.Up);
                    break;
                case Focuses.Difficulties:
                    uiController.diffSelect.OnDirectionInput(ControllerManager.Dir.Left);
                    break;
                case Focuses.Songs:
                    if (repeatCounter > 0)
                        uiController.UiSongScroller.OnDirectionInput(ControllerManager.Dir.Left);
                    else if (!uiController.UiSongScroller.IsScrolling.Value)
                        uiController.UiSongScroller.OnDirectionInput(ControllerManager.Dir.Up);
                    repeatCounter = 0.1f;
                    break;
            }
        }

        if (charCode == katsuRKey)
        {
            var uiController = GetUiSongScroller();
            switch (uiController.focus)
            {
                case Focuses.Filters:
                    uiController.filterScroller.OnDirectionInput(ControllerManager.Dir.Down);
                    break;
                case Focuses.Difficulties:
                    uiController.diffSelect.OnDirectionInput(ControllerManager.Dir.Right);
                    break;
                case Focuses.Songs:
                    if (repeatCounter > 0)
                        uiController.UiSongScroller.OnDirectionInput(ControllerManager.Dir.Right);
                    else if (!uiController.UiSongScroller.IsScrolling.Value)
                        uiController.UiSongScroller.OnDirectionInput(ControllerManager.Dir.Down);
                    break;
            }

            repeatCounter = 0.1f;
        }

        if (charCode == donLKey || charCode == donRKey)
        {
            var uiController = GetUiSongScroller();
            switch (uiController.focus)
            {
                case Focuses.Filters:
                    uiController.filterScroller.Decision();
                    break;
                case Focuses.Difficulties:
                    uiController.diffSelect.Decision();
                    break;
                case Focuses.Songs:
                    uiController.songScroller.Decision();
                    break;
            }
        }
    } // ReSharper disable Unity.PerformanceAnalysis

    private SongSelectSceneUiController GetUiSongScroller()
    {
        if (_cached != null) return _cached;
        var sceneObj = GameObject.Find("SongSelectSceneObjects")
            .GetComponent<SongSelectThunderShrineSceneObjects>();
        _cached = sceneObj.UiController;
        return _cached!;
    }

    private void UpdatePlaylistToQueuedSongList()
    {
        var uiController = GetUiSongScroller();
        Il2CppSystem.Collections.Generic.List<MusicDataInterface.MusicInfoAccesser> list = new();
        foreach (var info in LiveStreamSongSelectPanel.QueuedSongList)
            list.Add(info.SongInfo);
        var btns = uiController.songScroller.CreateItemList(list);
        uiController.songScroller.SelectItem(btns, true);
    }

    private void UpdateLiveStreamSongRequestStatus()
    {
        liveStreamSongRequestStatus.Text =
            $"直播功能已启用\n按下 O 键显示当前点歌列表\n按下 L 键从点歌歌单中删除所选歌曲\n当前已有 {LiveStreamSongSelectPanel.QueuedSongList.Count} 首点歌";
    }
}