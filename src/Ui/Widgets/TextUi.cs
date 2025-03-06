using UnityEngine;
#if BEPINEX
using Scripts.Common;
using TMPro;
using UtageExtensions;
#endif

#if MELONLOADER
using Il2CppScripts.Common;
using Il2CppTMPro;
using Il2CppUtageExtensions;
#endif

namespace TnTRFMod.Ui.Widgets;

public class TextUi : BaseUi
{
    private readonly TextMeshProUGUI _textTMP;
    private readonly UiText _uitext;

    public TextUi(bool useMainFont = false)
    {
        _textTMP = _go.AddComponent<TextMeshProUGUI>();
        _textTMP.enableWordWrapping = false;
        _uitext = _go.AddComponent<UiText>();
        TextColor = Color.white;
        if (useMainFont) UseMainFont();
        else UseDescriptionFont();
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

    public float FontSize
    {
        get => _textTMP.fontSize;
        set
        {
            _transform.SetHeight(value);
            _textTMP.fontSize = value;
        }
    }

    public TextAlignmentOptions Alignment
    {
        get => _textTMP.alignment;
        set => _textTMP.alignment = value;
    }

    public Color TextColor
    {
        get => _uitext.faceColor;
        set => _uitext.SetFaceColor(ref value);
    }

    private void UpdateText()
    {
        var color = Color.black;
        _uitext.SetOutlineColor(ref color);
        _uitext.faceDilate = 0.25f;
        _uitext.outlineWidth = 0.25f;
        _uitext.ApplyFont();
    }

    private void UseMainFont()
    {
        var fontMgr = Common.GetFontManager();
        var fontType = fontMgr.GetFontTypeBySystemLanguage();
        var fontAsset = fontMgr.GetDefaultFontAsset(fontType);
        var fontMat = fontMgr.GetDefaultFontMaterial(fontType, DataConst.DefaultFontMaterialType.OutlineBlack);

        _textTMP.font = fontAsset;
        _textTMP.material = fontMat;
        _uitext.tmpro = _textTMP;
        _uitext.SetCharacterSpacing(2f);
        UpdateText();
    }

    private void UseDescriptionFont()
    {
        var fontMgr = Common.GetFontManager();
        var fontType = fontMgr.GetFontTypeBySystemLanguage();
        var fontAsset = fontMgr.GetDescriptionFontAsset(fontType);
        var fontMat = fontMgr.GetDescriptionFontMaterial(fontType, DataConst.DescriptionFontMaterialType.OutlineBlack);

        _textTMP.font = fontAsset;
        _textTMP.material = fontMat;
        _uitext.tmpro = _textTMP;
        _uitext.SetCharacterSpacing(6f);
        UpdateText();
    }
}