using UnityEngine;
using UnityEngine.UI;

namespace TnTRFMod.Ui.Widgets;

public class ImageUi : BaseUi
{
    private readonly Sprite imageSprite;
    private readonly Texture2D imageTex;

    public ImageUi(byte[] imageData)
    {
        imageTex = LoadImage(imageData);
        imageSprite =
            Sprite.Create(imageTex, new Rect(0, 0, imageTex.width, imageTex.height), Vector2.zero,
                1f);
        imageSprite.name = "BaseImageSprite";

        Image = _go.AddComponent<Image>();

        Init();
    }

    public ImageUi(Texture2D imageTex)
    {
        this.imageTex = imageTex;
        imageSprite =
            Sprite.Create(imageTex, new Rect(0, 0, imageTex.width, imageTex.height), Vector2.zero,
                1f);
        imageSprite.name = "BaseImageSprite";

        Image = _go.AddComponent<Image>();

        Init();
    }

    public ImageUi(Sprite sprite)
    {
        imageTex = sprite.texture;
        imageSprite = sprite;
        imageSprite.name = "BaseImageSprite";

        Image = _go.AddComponent<Image>();

        Init();
    }

    public int TextureWidth => imageTex.width;
    public int TextureHeight => imageTex.height;
    public Image Image { get; }

    public static Texture2D LoadImage(byte[] imageData)
    {
        var imageTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        imageTex.LoadImage(imageData);
        return imageTex;
    }

    private void Init()
    {
        _transform.SetParent(Common.GetDrawCanvasForScene(), true);
        _transform.localScale = Vector3.one;
        Image.sprite = imageSprite;
        Size = imageSprite.rect.size;
    }
}