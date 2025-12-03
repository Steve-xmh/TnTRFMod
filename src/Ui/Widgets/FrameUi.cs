using UnityEngine;
using UnityEngine.UI;

namespace TnTRFMod.Ui.Widgets;

public class FrameUi : BaseUi
{
    private readonly Image _image;

    public FrameUi()
    {
        _image = _go.AddComponent<Image>();
        _image.sprite = baseUiSprite;
        _image.type = Image.Type.Sliced;
        _image.pixelsPerUnitMultiplier = 100;
    }

    public Color FrameColor
    {
        get => _image.color;
        set => _image.color = value;
    }
}