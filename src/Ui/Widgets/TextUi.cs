#if BEPINEX
using Scripts.Common;
using TMPro;
using UtageExtensions;
#endif

using TnTRFMod.Utils;
using UnityEngine;
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

    private DataConst.LanguageType? targetLanguageType;

    public TextUi(bool useMainFont = false)
    {
        _textTMP = _go.AddComponent<TextMeshProUGUI>();
        _textTMP.enableWordWrapping = false;
        _textTMP.extraPadding = true;
        _uitext = _go.AddComponent<UiText>();
        _uitext.tmpro = _textTMP;
        _uitext.SetUseRawText(true);
        Color = Color.white;
        if (useMainFont) UseMainFont();
        else UseDescriptionFont();
    }

    public string Text
    {
        get => _uitext.rawText;
        set
        {
            _uitext.SetTextRawOnly(value);
            UpdateText();
        }
    }

    public I18n.I18nResult I18nText
    {
        set => SetText(value);
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

    public float PreferredWidth => _uitext.GetPreferredWidth();
    public float PreferredHeight => _uitext.GetPreferredHeight();

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

    public bool WordWrap
    {
        get => _uitext.tmpro.enableWordWrapping;
        set
        {
            _uitext.tmpro.enableWordWrapping = value;
            _uitext.Refresh();
        }
    }

    public RectTransform Rect
    {
        get => _transform;
        set
        {
            _transform.anchorMin = value.anchorMin;
            _transform.anchorMax = value.anchorMax;
            _transform.pivot = value.pivot;
            _transform.sizeDelta = value.sizeDelta;
            _transform.anchoredPosition = value.anchoredPosition;
            _uitext.Refresh();
        }
    }

    public TextAlignmentOptions Alignment
    {
        get => _uitext.tmpro.alignment;
        set => _uitext.tmpro.alignment = value;
    }

    public void SetText(I18n.I18nResult format)
    {
        SetTargetLanguageType(format.LanguageType);
        Text = format.Text;
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
        _textTMP.fontSize = FontSize;
        _uitext.font = GetFontType();
        _uitext.SetOutlineColor(ref color);
        _uitext.faceDilate = 0.25f;
        _uitext.outlineWidth = 0.25f;
        _uitext.Refresh();
    }

    public void SetTargetLanguageType(DataConst.LanguageType? fontType)
    {
        if (targetLanguageType == fontType) return;
        targetLanguageType = fontType;
        UpdateText();
    }

    private DataConst.FontType GetFontType()
    {
        var fontMgr = Common.GetFontManager();
        var fontType = fontMgr.GetFontTypeBySystemLanguage();

        return targetLanguageType.HasValue ? fontMgr.GetFontType(targetLanguageType.Value) : fontType;
    }

    private void UseMainFont()
    {
        _uitext.font = GetFontType();
        _uitext.fontSetting = UiText.FontSetting.TaikoMain;
        _uitext.SetCharacterSpacing(2f);
        UpdateText();
    }

    private void UseDescriptionFont()
    {
        _uitext.font = GetFontType();
        _uitext.fontSetting = UiText.FontSetting.Description;
        _uitext.SetCharacterSpacing(6f);
        UpdateText();
    }
}