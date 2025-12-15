#if BEPINEX
using TMPro;
#elif MELONLOADER
using Il2CppTMPro;
#endif
using TnTRFMod.Patches;
using TnTRFMod.Ui.Widgets;
using UnityEngine;

namespace TnTRFMod.Scenes.Enso;

public class HitOffsetTip
{
    private EnsoGameManager _ensoGameManager;
    private TextUi hitOffset;

    private static float JudgeRange => Mathf.Approximately(TnTrfMod.Instance.hitOffsetRyoRange.Value, -1)
        ? EnsoGameBasePatch.PlayerStates[0].RyoJudgeRange
        : TnTrfMod.Instance.hitOffsetRyoRange.Value;

    private static Color FastColor => TnTrfMod.Instance.hitOffsetInvertColor.Value
        ? new Color32(248, 72, 40, 255)
        : new Color32(104, 192, 192, 255);

    private static Color LateColor => TnTrfMod.Instance.hitOffsetInvertColor.Value
        ? new Color32(104, 192, 192, 255)
        : new Color32(248, 72, 40, 255);

    public void Start()
    {
        _ensoGameManager = GameObject.Find("EnsoGameManager").GetComponent<EnsoGameManager>();
        hitOffset = new TextUi(true)
        {
            Name = "HitOffsetTip",
            Text = "0ms",
            FontSize = 48,
            Alignment = TextAlignmentOptions.TopRight,
            Position = new Vector2(470, 540)
        };
    }

    public void Update()
    {
        var time = (int)EnsoGameBasePatch.PlayerStates[0].LastHitTimeOffset;
        hitOffset.Text = $"{time}ms";
        if (time > JudgeRange)
            hitOffset.Color = FastColor;
        else if (time < -JudgeRange)
            hitOffset.Color = LateColor;
        else if (time == 0)
            hitOffset.Color = new Color32(109, 209, 111, 255);
        else
            hitOffset.Color = new Color32(248, 184, 0, 255);
    }
}