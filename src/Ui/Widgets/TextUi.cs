using Scripts.Common;
using TMPro;
using UnityEngine;
using UtageExtensions;

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
        var fontAsset = fontMgr.GetDescriptionFontAsset(fontType);
        // var fontMaterial = fontMgr.GetDescriptionFontMaterial(fontType, DataConst.DescriptionFontMaterialType.OutlineWhite);
        _textTMP.font = fontAsset;
        // _textTMP.material = fontMaterial;
        _uitext = _go.AddComponent<UiText>();
        _uitext.tmpro = _textTMP;
        var color = Color.white;
        _uitext.SetFaceColor(ref color);
        color = Color.black;
        _uitext.SetOutlineColor(ref color);
        _uitext.faceDilate = 0.25f;
        _uitext.outlineWidth = 0.25f;
        _uitext.tmpro.fontSize = 28;
        _uitext.SetCharacterSpacing(6f);
        _transform.SetHeight(28f);
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