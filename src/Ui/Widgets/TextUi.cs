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
    protected readonly TextMeshProUGUI _textTMP;
    protected readonly UiText _uitext;

    public TextUi(bool useMainFont = false)
    {
        _textTMP = _go.AddComponent<TextMeshProUGUI>();
        _textTMP.enableWordWrapping = false;
        _uitext = _go.AddComponent<UiText>();
        _uitext.tmpro = _textTMP;
        Color = Color.white;
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
        set
        {
            _uitext.SetFaceColor(ref value);
            _uitext.Refresh();
        }
    }

    public float FontSize
    {
        get => _uitext.tmpro.fontSize;
        set
        {
            _transform.SetHeight(value);
            _uitext.tmpro.fontSize = value;
            _uitext.Refresh();
        }
    }

    public TextAlignmentOptions Alignment
    {
        get => _uitext.tmpro.alignment;
        set => _uitext.tmpro.alignment = value;
    }

    public void SetText(string format, float value0)
    {
        _uitext.tmpro.SetText(format, value0);
    }
    
    public void SetText(string format, float value0, float value1)
    {
        _uitext.tmpro.SetText(format, value0, value1);
    }

    private void UpdateText()
    {
        var color = Color.black;
        _uitext.SetOutlineColor(ref color);
        _uitext.faceDilate = 0.25f;
        _uitext.outlineWidth = 0.25f;
        _uitext.ApplyFont();
        _uitext.Refresh();
    }

    private void UseMainFont()
    {
        var fontMgr = Common.GetFontManager();
        var fontType = fontMgr.GetFontTypeBySystemLanguage();
        // var fontAsset = fontMgr.GetDefaultFontAsset(fontType);
        // var fontMat = fontMgr.GetDefaultFontMaterial(fontType, DataConst.DefaultFontMaterialType.OutlineBlack);
        //
        // _textTMP.font = fontAsset;
        // _textTMP.material = fontMat;
        // _uitext.tmpro = _textTMP;
        _uitext.font = fontType;
        _uitext.fontSetting = UiText.FontSetting.TaikoMain;
        _uitext.SetCharacterSpacing(2f);
        UpdateText();
    }

    private void UseDescriptionFont()
    {
        var fontMgr = Common.GetFontManager();
        var fontType = fontMgr.GetFontTypeBySystemLanguage();
        // var fontAsset = fontMgr.GetDescriptionFontAsset(fontType);
        // var fontMat = fontMgr.GetDescriptionFontMaterial(fontType, DataConst.DescriptionFontMaterialType.OutlineBlack);

        _uitext.font = fontType;
        _uitext.fontSetting = UiText.FontSetting.Description;
        _uitext.SetCharacterSpacing(6f);
        UpdateText();
    }
}