#if BEPINEX
using TMPro;
#elif MELONLOADER
using Il2CppTMPro;
#endif
using TnTRFMod.Ui.Widgets;
using UnityEngine;

namespace TnTRFMod.Ui.Tokkun;

public class DrumButton : BaseUi
{
    private readonly ImageUi buttonImage;
    private readonly TextUi buttonTopLabel;
    private BaseUi buttonAction;

    public DrumButton(bool isKatsu = false)
    {
        Name = isKatsu ? "DrumButtonKatsu" : "DrumButtonDon";

        buttonImage =
            new ImageUi(isKatsu ? TextureManager.Textures.TokkunButtonKatsu : TextureManager.Textures.TokkunButtonDon);
        buttonTopLabel = new TextUi(true);
        var action = new TextUi(true);
        buttonTopLabel.Text = "Label";
        action.Text = "字";
        action.FontSize = 140f;
        action.Alignment = TextAlignmentOptions.Center;
        buttonAction = action;

        AddChild(buttonImage);
        AddChild(buttonTopLabel);
        AddChild(buttonAction);

        buttonTopLabel._transform.pivot = new Vector2(1f, 0f);
        buttonTopLabel._transform.sizeDelta = Vector2.zero;
        buttonTopLabel.Alignment = TextAlignmentOptions.Center;
        buttonAction._transform.pivot = new Vector2(1f, 0f);
        buttonAction._transform.sizeDelta = Vector2.zero;

        buttonTopLabel._transform.localPosition = new Vector2(100f, -35f);
        buttonAction._transform.localPosition = new Vector2(0f, -60f);
    }

    public void SetLabel(string label)
    {
        buttonTopLabel.Text = label;
    }

    public void SetActionText(string text)
    {
        buttonAction.Dispose();
        var action = new TextUi(true)
        {
            Text = text,
            Alignment = TextAlignmentOptions.Center
        };
        AddChild(action);
        action.FontSize = 70f;
        buttonAction = action;
        buttonAction._transform.pivot = new Vector2(1f, 0f);
        buttonAction._transform.sizeDelta = Vector2.zero;
        buttonAction._transform.localPosition = new Vector2(0f, -60f);
    }

    public void SetActionIcon(Sprite sprite)
    {
        buttonAction.Dispose();
        var action = new ImageUi(sprite);
        AddChild(action);
        action._transform.pivot = new Vector2(0.5f, 0.5f);
        action._transform.localScale = new Vector2(1f, 1f);
        action._transform.localPosition = new Vector2(0f, -65.625f);
        buttonAction = action;
    }
}