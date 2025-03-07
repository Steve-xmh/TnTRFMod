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
            Text = "音符命中率",
            FontSize = 50
        };
        trainCounterUi.AddChild(hitAspectLabel);
        hitAspectLabel.Position = new Vector2(33, 40);

        var aspectTextColor = new Color32(219, 150, 0, 255);

        var rendaLabel = new TextUi(true)
        {
            Text = "连打",
            FontSize = 40,
            Color = new Color32(255, 255, 35, 255)
        };
        trainCounterUi.AddChild(rendaLabel);
        rendaLabel.Position = new Vector2(51, 338);

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

        // Aspects

        const int AspectAlignX = 238;

        hitAspectValue = new TextUi(true)
        {
            Text = "100%",
            FontSize = 50,
            Color = aspectTextColor,
            Alignment = TextAlignmentOptions.TopRight
        };
        trainCounterUi.AddChild(hitAspectValue);
        hitAspectValue.Position = new Vector2(AspectAlignX, 40);

        ryoAspect = new TextUi(true)
        {
            Text = "100",
            FontSize = 48,
            Color = aspectTextColor,
            Alignment = TextAlignmentOptions.TopRight
        };
        trainCounterUi.AddChild(ryoAspect);
        ryoAspect.Position = new Vector2(AspectAlignX, 123);

        kaAspect = new TextUi(true)
        {
            Text = "0",
            FontSize = 48,
            Color = aspectTextColor,
            Alignment = TextAlignmentOptions.TopRight
        };
        trainCounterUi.AddChild(kaAspect);
        kaAspect.Position = new Vector2(AspectAlignX, 180);

        fukaAspect = new TextUi(true)
        {
            Text = "0",
            FontSize = 48,
            Color = aspectTextColor,
            Alignment = TextAlignmentOptions.TopRight
        };
        trainCounterUi.AddChild(fukaAspect);
        fukaAspect.Position = new Vector2(AspectAlignX, 237);

        // Counters

        const int CounterAlignX = 90;

        ryoCounter = new TextUi(true)
        {
            Text = "0",
            FontSize = 48,
            Alignment = TextAlignmentOptions.TopRight
        };
        trainCounterUi.AddChild(ryoCounter);
        ryoCounter.Position = new Vector2(CounterAlignX, 123);

        kaCounter = new TextUi(true)
        {
            Text = "0",
            FontSize = 48,
            Alignment = TextAlignmentOptions.TopRight
        };
        trainCounterUi.AddChild(kaCounter);
        kaCounter.Position = new Vector2(CounterAlignX, 180);

        fukaCounter = new TextUi(true)
        {
            Text = "0",
            FontSize = 48,
            Alignment = TextAlignmentOptions.TopRight
        };
        trainCounterUi.AddChild(fukaCounter);
        fukaCounter.Position = new Vector2(CounterAlignX, 237);

        rendaCounter = new TextUi(true)
        {
            Text = "0",
            FontSize = 48,
            Alignment = TextAlignmentOptions.TopRight
        };
        trainCounterUi.AddChild(rendaCounter);
        rendaCounter.Position = new Vector2(CounterAlignX, 333);

        hitOffset = new TextUi(true)
        {
            Text = "0ms",
            FontSize = 48,
            Alignment = TextAlignmentOptions.TopRight
        };
        trainCounterUi.AddChild(hitOffset);
        hitOffset.Position = new Vector2(470, -38);
    }

    public void Update()
    {
        var ryo = ShowJudgeOffsetPatch.RyoCount;
        var ka = ShowJudgeOffsetPatch.KaCount;
        var fuka = ShowJudgeOffsetPatch.FuKaCount;
        var time = (int)ShowJudgeOffsetPatch.LastHitTimeOffset;

        var total = ryo + ka + fuka;

        ryoCounter.Text = ryo.ToString();
        kaCounter.Text = ka.ToString();
        fukaCounter.Text = fuka.ToString();
        hitOffset.Text = $"{time}ms";
        hitOffset.Color = time switch
        {
            > 0 => Color.yellow,
            < 0 => Color.red,
            _ => Color.green
        };

        if (total > 0)
        {
            var totalRyoAndKa = ryo + ka;
            if (fuka == 0)
            {
                var ryoAspectValue = (int)(100 * (ryo / (float)totalRyoAndKa));
                ryoAspect.Text = $"{ryoAspectValue}%";
                kaAspect.Text = $"{100 - ryoAspectValue}%";
                fukaAspect.Text = "0%";
                hitAspectValue.Text = "100%";
            }
            else
            {
                var ryoAspectValue = (int)(100 * (ryo / (float)total));
                var kaAspectValue = (int)(100 * (ka / (float)total));
                var fukaAspectValue = 100 - ryoAspectValue - kaAspectValue;
                ryoAspect.Text = $"{ryoAspectValue}%";
                kaAspect.Text = $"{kaAspectValue}%";
                fukaAspect.Text = $"{fukaAspectValue}%";
                hitAspectValue.Text = $"{100 - fukaAspectValue}%";
            }
        }
        else
        {
            hitAspectValue.Text = "100%";
            ryoAspect.Text = "100%";
            kaAspect.Text = "0%";
            fukaAspect.Text = "0%";
            hitAspectValue.Text = "100%";
        }

        rendaCounter.Text = ShowJudgeOffsetPatch.RendaCount.ToString();
    }

    public void HideTrainCounter()
    {
        trainCounterUi.SetActive(false);
    }
}