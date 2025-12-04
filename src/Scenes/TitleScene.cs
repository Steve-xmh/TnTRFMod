using TnTRFMod.Config;
using TnTRFMod.Patches;
using TnTRFMod.Ui;
using TnTRFMod.Ui.Widgets;
using TnTRFMod.Utils;
using UnityEngine;

namespace TnTRFMod.Scenes;

public class TitleScene : IScene
{
    private LoggingScreenUi.LogHandle? firstRunLogHandle;
    private bool isAutoSongDownloaded;
    public string SceneName => "Title";

    public void Start()
    {
        _ = new TextUi
        {
            Text = $"{TnTrfMod.MOD_NAME} v{TnTrfMod.MOD_VERSION} ({TnTrfMod.MOD_LOADER})",
            Position = new Vector2(64f, 64f)
        };

        if (ConfigEntry.IsFirstConfig)
            firstRunLogHandle = LoggingScreenUi.New(I18n.Get("title.firstRunTip", ConfigEntry.ConfigFilePath).Text);

        if (!isAutoSongDownloaded)
        {
            isAutoSongDownloaded = true;

            Task.Run(AutoDownloadSubscriptionSongs.StartAutoDownloadSubscriptionSongsAsync);
        }
    }

    public void Destroy()
    {
        firstRunLogHandle?.Dispose();
    }
}