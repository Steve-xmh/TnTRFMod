using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if BEPINEX
using TMPro;
#endif

#if MELONLOADER
using Il2CppTMPro;
#endif

namespace TnTRFMod.Ui.Widgets;

public class TextFieldUi : BaseUi
{
    private readonly Image _image;
    private readonly TextUi _placeHolderText;
    private readonly TextUi _text;
    private readonly TMP_InputField inputField;

    public TextFieldUi()
    {
        _image = _go.AddComponent<Image>();
        _image.sprite = baseUiSprite;
        _image.type = Image.Type.Sliced;
        _image.pixelsPerUnitMultiplier = 100;

        _text = new TextUi
        {
            Name = "InputText",
            FontSize = 20
        };
        AddChild(_text);
        _text.Rect.anchorMin = Vector2.zero;
        _text.Rect.anchorMax = Vector2.one;
        _text.Rect.offsetMin = new Vector2(15, 15);
        _text.Rect.offsetMax = new Vector2(-15, -15);

        _placeHolderText = new TextUi
        {
            Name = "PlaceholderText",
            FontSize = 20,
            Text = "请输入文本",
            Color = new Color(0.5f, 0.5f, 0.5f, 0.5f)
        };
        AddChild(_placeHolderText);
        _placeHolderText.Rect.anchorMin = Vector2.zero;
        _placeHolderText.Rect.anchorMax = Vector2.one;
        _placeHolderText.Rect.offsetMin = new Vector2(15, 15);
        _placeHolderText.Rect.offsetMax = new Vector2(-15, -15);

        inputField = _go.AddComponent<TMP_InputField>();
        inputField.customCaretColor = true;
        inputField.caretColor = Color.black;
        inputField.textComponent = _text._go.GetComponent<TextMeshProUGUI>();
        inputField.interactable = true;
        var placeholder = _placeHolderText._go.GetComponent<TextMeshProUGUI>();
        inputField.placeholder = placeholder;
        inputField.textComponent.enableWordWrapping = false;
        placeholder.enableWordWrapping = false;

        inputField.onSelect.AddListener(DelegateSupport.ConvertDelegate<UnityAction<string>>((string text) =>
        {
            ControllerManager.Instance.DisablePlayerController(ControllerManager.ControllerPlayerNo.Player1);
        }));

        inputField.onDeselect.AddListener(DelegateSupport.ConvertDelegate<UnityAction<string>>((string text) =>
        {
            ControllerManager.Instance.EnablePlayerController(ControllerManager.ControllerPlayerNo.Player1);
        }));
    }

    public string Value
    {
        get => inputField.text;
        set => inputField.text = value;
    }

    public string Placeholder
    {
        get => _placeHolderText.Text;
        set => _placeHolderText.Text = value;
    }

    public Color TextColor
    {
        get => _text.Color;
        set => _text.Color = value;
    }

    public Color PlaceholderColor
    {
        get => _placeHolderText.Color;
        set => _placeHolderText.Color = value;
    }

    public Color BackgroundColor
    {
        get => _image.color;
        set => _image.color = value;
    }

    public void AddOnValueChangedListener(Action<string> action)
    {
        inputField.onValueChanged.AddListener(DelegateSupport.ConvertDelegate<UnityAction<string>>(action));
    }

    public void AddOnEndEditListener(Action<string> action)
    {
        inputField.onEndEdit.AddListener(DelegateSupport.ConvertDelegate<UnityAction<string>>(action));
    }
}