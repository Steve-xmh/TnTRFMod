using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TnTRFMod.Ui.Widgets;

public class ButtonUi : BaseUi
{
    private readonly Button _button;
    private readonly Image _image;
    private readonly TextUi _label;

    public ButtonUi()
    {
        _transform.parent = Common.GetDrawCanvas();
        _transform.pivot = new Vector2(0, 1);

        _image = _go.AddComponent<Image>();
        _image.sprite = baseUiSprite;
        _image.type = Image.Type.Sliced;
        _image.pixelsPerUnitMultiplier = 100;

        _button = _go.AddComponent<Button>();

        Size = new Vector2(160, 30);

        _label = new TextUi
        {
            Text = "按钮"
            // _transform =
            // {
            //     anchorMin = new Vector2(0, 0),
            //     anchorMax = new Vector2(1, 1),
            //     pivot = new Vector2(0.5f, 0.5f)
            // },
            // Alignment = TextAlignmentOptions.Center
        };
        AddChild(_label);
    }

    public Vector2 Position
    {
        get
        {
            var pos = _transform.anchoredPosition;
            return new Vector2(pos.x + Common.ScreenWidth / 2f, Common.ScreenHeight / 2f - pos.y);
        }
        set => _transform.anchoredPosition =
            new Vector2(value.x - Common.ScreenWidth / 2f, Common.ScreenHeight / 2f - value.y);
    }

    public Vector2 Size
    {
        get => _transform.sizeDelta;
        set => _transform.sizeDelta = value;
    }

    public string Text
    {
        get => _label.Text;
        set => _label.Text = value;
    }

    public void AddListener(Delegate action)
    {
        _button.onClick.AddListener(DelegateSupport.ConvertDelegate<UnityAction>(action));
    }
}