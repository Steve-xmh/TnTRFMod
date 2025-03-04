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
            Text = $"TnTRFMod v{MyPluginInfo.PLUGIN_VERSION} (BepInEx)",
            Position = new Vector2(64f, 64f)
        };

        if (isAutoSongDownloaded) return;
        isAutoSongDownloaded = true;
        TnTrfMod.Instance.StartCoroutine(new AutoDownloadSubscriptionSongs().StartAutoDownloadSubscriptionSongs());
    }

    public void Update()
    {
    }
}