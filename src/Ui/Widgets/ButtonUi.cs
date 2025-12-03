using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

namespace TnTRFMod.Ui.Widgets;

public class ButtonUi : BaseUi
{
    private readonly Button _button;
    private readonly Image _image;
    private readonly TextUi _label;

    public ButtonUi()
    {
        _transform.pivot = new Vector2(0, 1);

        _image = _go.AddComponent<Image>();
        _image.sprite = baseUiSprite;
        _image.type = Image.Type.Sliced;
        _image.pixelsPerUnitMultiplier = 100;

        _button = _go.AddComponent<Button>();

        Size = new Vector2(160, 28 + 6);

        _label = new TextUi
        {
            Text = "按钮",
            FontSize = 20,
            Alignment = TextAlignmentOptions.Center
        };
        AddChild(_label);
        _label.Rect.anchorMin = Vector2.zero;
        _label.Rect.anchorMax = Vector2.one;
        _label.Rect.offsetMin = new Vector2(15, 15);
        _label.Rect.offsetMax = new Vector2(-15, -15);
    }

    // public new Vector2 Position
    // {
    //     get
    //     {
    //         var pos = _transform.anchoredPosition;
    //         return new Vector2(pos.x + Common.ScreenWidth / 2f, Common.ScreenHeight / 2f - pos.y);
    //     }
    //     set => _transform.anchoredPosition =
    //         new Vector2(value.x - Common.ScreenWidth / 2f, Common.ScreenHeight / 2f - value.y);
    // }

    public new Vector2 Size
    {
        get => _transform.sizeDelta;
        set => _transform.sizeDelta = value;
    }

    public string Text
    {
        get => _label.Text;
        set => _label.Text = value;
    }

    public Color ButtonColor
    {
        get => _image.color;
        set => _image.color = value;
    }

    public Color TextColor
    {
        get => _label.Color;
        set => _label.Color = value;
    }

    public void AddListener(Delegate action)
    {
        _button.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(action));
    }
}