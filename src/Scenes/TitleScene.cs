using TnTRFMod.Ui.Widgets;
using UnityEngine;

namespace TnTRFMod.Ui.Scenes;

public class TitleScene : IScene
{
    public string SceneName => "Title";

    public void Start()
    {
        _ = new TextUi
        {
            Text = $"TnTRFMod v{MyPluginInfo.PLUGIN_VERSION} (BepInEx)",
            Position = new Vector2(32f, 32f)
        };
    }
}