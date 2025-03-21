using TnTRFMod.Patches;
using TnTRFMod.Ui.Widgets;
using UnityEngine;

namespace TnTRFMod.Scenes;

public class TitleScene : IScene
{
    private bool isAutoSongDownloaded;
    public string SceneName => "Title";

    public void Start()
    {
        _ = new TextUi
        {
            Text = $"{TnTrfMod.MOD_NAME} v{TnTrfMod.MOD_VERSION} ({TnTrfMod.MOD_LOADER})",
            Position = new Vector2(64f, 64f)
        };

        if (isAutoSongDownloaded) return;
        isAutoSongDownloaded = true;

        _ = new AutoDownloadSubscriptionSongs().StartAutoDownloadSubscriptionSongsAsync();
    }
}