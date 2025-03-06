using UnityEngine;
using UnityEngine.UI;

namespace TnTRFMod.Ui.Widgets;

public class ImageUi : BaseUi
{
    private readonly Image _image;
    private readonly Sprite imageSprite;
    private readonly Texture2D imageTex;

    public ImageUi(byte[] imageData, int imageWidth, int imageHeight)
    {
        imageTex = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false);
        imageTex.LoadImage(imageData);
        imageSprite =
            Sprite.Create(imageTex, new Rect(0, 0, imageTex.width, imageTex.height), Vector2.zero,
                1f);
        imageSprite.name = "BaseImageSprite";

        _image = _go.AddComponent<Image>();

        Init();
    }

    public ImageUi(Texture2D imageTex)
    {
        this.imageTex = imageTex;
        imageSprite =
            Sprite.Create(imageTex, new Rect(0, 0, imageTex.width, imageTex.height), Vector2.zero,
                1f);
        imageSprite.name = "BaseImageSprite";

        _image = _go.AddComponent<Image>();

        Init();
    }

    private void Init()
    {
        _transform.parent = Common.GetDrawCanvasForScene();
        _image.sprite = imageSprite;
        Size = imageSprite.rect.size;
    }
}