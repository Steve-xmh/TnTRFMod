using TMPro;
using UnityEngine;

namespace TnTRFMod.Ui.Widgets;

public class TextFieldUi : TextUi
{
    private readonly TMP_InputField inputField;

    public TextFieldUi()
    {
        inputField = _go.AddComponent<TMP_InputField>();
        inputField.textComponent = _textTMP;
        inputField.ActivateInputField();
        Size = new Vector2(200, 25);
        Position = new Vector2(200, 200);
        // var placeHolder = new TextUi();
        // placeHolder.Text = "Search";
        // placeHolder.Color = Color.gray;
        // inputField.placeholder = placeHolder._go.GetComponent<Graphic>();
    }
}