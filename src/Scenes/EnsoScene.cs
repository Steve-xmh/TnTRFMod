using TMPro;
using TnTRFMod.Patches;
using TnTRFMod.Ui.Widgets;
using UnityEngine;

namespace TnTRFMod.Scenes;

public class EnsoScene : IScene
{
    private TextUi fukaAspect;
    private TextUi fukaCounter;

    private TextUi hitAspectValue;
    private TextUi hitOffset;
    private TextUi kaAspect;
    private TextUi kaCounter;

    private TextUi rendaCounter;

    private TextUi ryoAspect;

    private TextUi ryoCounter;
    private ImageUi trainCounterUi;

    public string SceneName => "Enso";

    public void Init()
    {
    }

    public void Start()
    {
        NoShadowOnpuPatch.CheckOrInitializePatch();
        BufferedNoteInputPatch.ResetCounts();

        if (TnTrfMod.Instance.enableNearestNeighborOnpuPatch.Value) NearestNeighborOnpuPatch.PatchLaneTarget();

        trainCounterUi = new ImageUi(Resources.TrainCounter, 325, 280)
        {
            Position = new Vector2(37.5f, 577.5f),
            Name = "TrainCounterSprite"
        };

        var hitAspectLabel = new TextUi(true)
        {
            Text = "击打命中率",
            FontSize = 50
        };
        trainCounterUi.AddChild(hitAspectLabel);
        hitAspectLabel.Position = new Vector2(33, 40);

        var rendaLabel = new TextUi(true)
        {
            Text = "连打",
            FontSize = 40,
            TextColor = new Color(1f, 1f, 0.13725491f)
        };
        trainCounterUi.AddChild(rendaLabel);
        rendaLabel.Position = new Vector2(51, 338);

        hitAspectValue = new TextUi(true)
        {
            Text = "100",
            FontSize = 50,
            Alignment = TextAlignmentOptions.TopRight
        };
        trainCounterUi.AddChild(hitAspectValue);
        hitAspectValue.Position = new Vector2(325 - 100, 40);

        const int LabelAlignX = 130;

        var ryo = new ImageUi(Resources.HitRyo, 44, 53);
        trainCounterUi.AddChild(ryo);
        ryo.Position = new Vector2(LabelAlignX - ryo.Size.x, 121);
        var ka = new ImageUi(Resources.HitKa, 47, 51);
        trainCounterUi.AddChild(ka);
        ka.Position = new Vector2(LabelAlignX - ka.Size.x, 179);
        var fuka = new ImageUi(Resources.HitFuka, 86, 51);
        trainCounterUi.AddChild(fuka);
        fuka.Position = new Vector2(LabelAlignX - fuka.Size.x, 236);

        ryoCounter = new TextUi(true)
        {
            Text = "0",
            FontSize = 48,
            Alignment = TextAlignmentOptions.TopRight
        };
        trainCounterUi.AddChild(ryoCounter);
        ryoCounter.Position = new Vector2(325 - 100, 123);

        kaCounter = new TextUi(true)
        {
            Text = "0",
            FontSize = 48,
            Alignment = TextAlignmentOptions.TopRight
        };
        trainCounterUi.AddChild(kaCounter);
        kaCounter.Position = new Vector2(325 - 100, 180);

        fukaCounter = new TextUi(true)
        {
            Text = "0",
            FontSize = 48,
            Alignment = TextAlignmentOptions.TopRight
        };
        trainCounterUi.AddChild(fukaCounter);
        fukaCounter.Position = new Vector2(325 - 100, 237);

        // Aspects

        ryoAspect = new TextUi(true)
        {
            Text = "100",
            FontSize = 48,
            Alignment = TextAlignmentOptions.TopRight
        };
        trainCounterUi.AddChild(ryoAspect);
        ryoAspect.Position = new Vector2(45, 123);

        kaAspect = new TextUi(true)
        {
            Text = "0",
            FontSize = 48,
            Alignment = TextAlignmentOptions.TopRight
        };
        trainCounterUi.AddChild(kaAspect);
        kaAspect.Position = new Vector2(45, 180);

        fukaAspect = new TextUi(true)
        {
            Text = "0",
            FontSize = 48,
            Alignment = TextAlignmentOptions.TopRight
        };
        trainCounterUi.AddChild(fukaAspect);
        fukaAspect.Position = new Vector2(45, 237);

        rendaCounter = new TextUi(true)
        {
            Text = "0",
            FontSize = 48,
            Alignment = TextAlignmentOptions.TopRight
        };
        trainCounterUi.AddChild(rendaCounter);
        rendaCounter.Position = new Vector2(109, 333);

        hitOffset = new TextUi(true)
        {
            Text = "0ms",
            FontSize = 48,
            Alignment = TextAlignmentOptions.TopRight
        };
        trainCounterUi.AddChild(hitOffset);
        hitOffset.Position = new Vector2(470, 38);
    }

    public void Update()
    {
        var ryo = ShowJudgeOffsetPatch.RyoCount;
        var ka = ShowJudgeOffsetPatch.KaCount;
        var fuka = ShowJudgeOffsetPatch.FuKaCount;

        var total = ryo + ka + fuka;

        ryoCounter.Text = ryo.ToString();
        kaCounter.Text = ka.ToString();
        fukaCounter.Text = fuka.ToString();
        hitOffset.Text = $"{(int)ShowJudgeOffsetPatch.LastHitTimeOffset}ms";

        if (total > 0)
        {
            var ryoAspectValue = (int)(100 * (ryo / (float)total));
            var kaAspectValue = (int)(100 * (ka / (float)total));
            ryoAspect.Text = ryoAspectValue.ToString();
            kaAspect.Text = kaAspectValue.ToString();
            fukaAspect.Text = (100 - ryoAspectValue - kaAspectValue).ToString();
        }

        rendaCounter.Text = ShowJudgeOffsetPatch.RendaCount.ToString();

        hitAspectValue.Text = total == 0 ? "100" : ((int)(100 * (ryo + ka) / (float)total)).ToString();
    }

    public void HideTrainCounter()
    {
        trainCounterUi.SetActive(false);
    }
}