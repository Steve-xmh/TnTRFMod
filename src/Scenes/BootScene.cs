using TnTRFMod.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace TnTRFMod.Scenes;

public class BootScene : IScene
{
    public string SceneName => "Boot";

    public void Start()
    {
        var blackGo = new GameObject("BlackGo");
        var transform = blackGo.AddComponent<RectTransform>();
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.black);
        texture.Apply();
        transform.SetParent(Common.GetDrawCanvasForScene());
        transform.pivot = new Vector2(0, 1);
        transform.anchoredPosition =
            new Vector2(Common.ScreenWidth / -2f, Common.ScreenHeight / 2f);
        transform.sizeDelta = new Vector2(1920, 1080);
        var image = blackGo.AddComponent<Image>();
        image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero, 1f);
    }
}