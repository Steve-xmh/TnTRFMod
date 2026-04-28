using TnTRFMod.Ui.Widgets;
using UnityEngine;
#if BEPINEX
using TMPro;
#elif MELONLOADER
using Il2CppTMPro;
#endif

namespace TnTRFMod.Ui.Tokkun;

public class DrumButton : BaseUi
{
    private static readonly Vector2 ActionTextLocalPos = new(0f, -60f);
    private static readonly Vector2 ActionIconLocalPos = new(0f, -65.625f);
    private static readonly Vector2 TopLabelLocalPos = new(100f, -35f);

    private readonly ImageUi buttonImage;
    private readonly TextUi buttonTopLabel;
    private TextUi? _actionText;
    private ImageUi? _actionIcon;

    public DrumButton(bool isKatsu = false)
    {
        Name = isKatsu ? "DrumButtonKatsu" : "DrumButtonDon";

        buttonImage =
            new ImageUi(isKatsu ? TextureManager.Textures.TokkunButtonKatsu : TextureManager.Textures.TokkunButtonDon);
        buttonTopLabel = new TextUi(true);
        buttonTopLabel.Text = "Label";

        _actionText = new TextUi(true)
        {
            Text = "字",
            FontSize = 140f,
            Alignment = TextAlignmentOptions.Center
        };

        AddChild(buttonImage);
        AddChild(buttonTopLabel);
        AddChild(_actionText);

        buttonTopLabel._transform.pivot = new Vector2(1f, 0f);
        buttonTopLabel._transform.sizeDelta = Vector2.zero;
        buttonTopLabel.Alignment = TextAlignmentOptions.Center;
        _actionText._transform.pivot = new Vector2(1f, 0f);
        _actionText._transform.sizeDelta = Vector2.zero;

        buttonTopLabel._transform.localPosition = TopLabelLocalPos;
        _actionText._transform.localPosition = ActionTextLocalPos;
    }

    public void SetLabel(string label)
    {
        buttonTopLabel.Text = label;
    }

    public void SetActionText(string text)
    {
        if (_actionIcon != null) _actionIcon.Visible = false;
        if (_actionText == null)
        {
            _actionText = new TextUi(true)
            {
                FontSize = 70f,
                Alignment = TextAlignmentOptions.Center
            };
            AddChild(_actionText);
            _actionText._transform.pivot = new Vector2(1f, 0f);
            _actionText._transform.sizeDelta = Vector2.zero;
            _actionText._transform.localPosition = ActionTextLocalPos;
        }

        _actionText.Text = text;
        _actionText.Visible = true;
    }

    public void SetActionIcon(Sprite sprite)
    {
        if (_actionText != null) _actionText.Visible = false;
        if (_actionIcon == null)
        {
            _actionIcon = new ImageUi(sprite);
            AddChild(_actionIcon);
            _actionIcon._transform.pivot = new Vector2(0.5f, 0.5f);
            _actionIcon._transform.localScale = new Vector2(1f, 1f);
            _actionIcon._transform.localPosition = ActionIconLocalPos;
        }
        else
        {
            _actionIcon.Image.sprite = sprite;
        }

        _actionIcon.Visible = true;
    }
}