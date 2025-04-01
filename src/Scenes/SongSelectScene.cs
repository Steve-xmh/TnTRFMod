using TnTRFMod.Scenes.Enso;
using TnTRFMod.Ui.Widgets;
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
    private TextUi liveStreamSongRequestStatus;
    public string SceneName => "SongSelect";

    public void Start()
    {
        if (TnTrfMod.Instance.enableBilibiliLiveStreamSongRequest.Value)
        {
            liveStreamSongRequestStatus = new TextUi
            {
                FontSize = 20,
                Position = new Vector2(1570f, 863f)
            };
            UpdateLiveStreamSongRequestStatus();
        }

        var searchTextInput = new TextFieldUi();
        searchTextInput.Name = "SearchTextInput";
    }

    public void Update()
    {
        if (!TnTrfMod.Instance.enableBilibiliLiveStreamSongRequest.Value) return;
        if (Keyboard.current[Key.K].wasPressedThisFrame)
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

    private SongSelectSceneUiController GetUiSongScroller()
    {
        var sceneObj = GameObject.Find("SongSelectSceneObjects")
            .GetComponent<SongSelectThunderShrineSceneObjects>();
        return sceneObj.UiController;
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
            $"直播功能已启用\n按下 K 键显示当前点歌列表\n按下 L 键从点歌歌单中删除所选歌曲\n当前已有 {LiveStreamSongSelectPanel.QueuedSongList.Count} 首点歌";
    }
}