using Scripts.OutGame.SongSelect;
using TnTRFMod.Scenes.Enso;
using TnTRFMod.Ui.Widgets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TnTRFMod.Scenes;

public class SongSelectScene : IScene
{
    private TextUi liveStreamSongRequestStatus;
    public string SceneName => "SongSelect";

    public void Start()
    {
        liveStreamSongRequestStatus = new TextUi
        {
            FontSize = 20,
            Position = new Vector2(1570f, 863f)
        };
        UpdateLiveStreamSongRequestStatus();
    }

    public void Update()
    {
        if (Keyboard.current[Key.K].wasPressedThisFrame)
        {
            var sceneObj = GameObject.Find("SongSelectSceneObjects")
                .GetComponent<SongSelectThunderShrineSceneObjects>();
            var uiController = sceneObj.UiController;
            Il2CppSystem.Collections.Generic.List<MusicDataInterface.MusicInfoAccesser> list = new();
            foreach (var info in LiveStreamSongSelectPanel.QueuedSongList)
                list.Add(info.SongInfo);
            var btns = uiController.songScroller.CreateItemList(list);
            uiController.songScroller.SelectItem(btns, true);
        }

        UpdateLiveStreamSongRequestStatus();
    }

    private void UpdateLiveStreamSongRequestStatus()
    {
        liveStreamSongRequestStatus.Text =
            $"直播功能已启用\n按下 K 键显示当前点歌列表\n当前已有 {LiveStreamSongSelectPanel.QueuedSongList.Count} 首点歌";
    }
}