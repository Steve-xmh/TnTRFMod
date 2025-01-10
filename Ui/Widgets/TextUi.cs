using Il2CppScripts.Common;
using Il2CppTMPro;
using Il2CppUtageExtensions;
using UnityEngine;

namespace TnTRFMod.Ui.Widgets;

public class TextUi : BaseUi
{
    private readonly TextMeshProUGUI _textTMP;
    private readonly UiText _uitext;

    public TextUi()
    {
        _textTMP = _go.AddComponent<TextMeshProUGUI>();
        _textTMP.enableWordWrapping = false;
        var fontMgr = Common.GetFontManager();
        var fontType = fontMgr.GetFontTypeBySystemLanguage();
        var fontAsset = fontMgr.GetDefaultFontAsset(fontType);
        _textTMP.font = fontAsset;
        _uitext = _go.AddComponent<UiText>();
        _uitext.tmpro = _textTMP;
        var color = Color.white;
        _uitext.SetFaceColor(ref color);
        color = Color.black;
        _uitext.SetOutlineColor(ref color);
        _uitext.outlineWidth = 0.2f;
        _uitext.tmpro.fontSize = 24;
        _transform.SetHeight(24f);
    }

    public string Text
    {
        get => _uitext.rawText;
        set => _uitext.SetTextRaw(value);
    }

    public Color Color
    {
        get => _uitext.faceColor;
        set => _uitext.SetFaceColor(ref value);
    }

    public TextAlignmentOptions Alignment
    {
        get => _uitext.tmpro.alignment;
        set => _uitext.tmpro.alignment = value;
    }
}