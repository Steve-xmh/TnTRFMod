using TnTRFMod.Patches;
using TnTRFMod.Ui;
using TnTRFMod.Ui.Widgets;
using TnTRFMod.Utils;
using UnityEngine;
#if BEPINEX
using TMPro;
#endif

#if MELONLOADER
using Il2CppTMPro;
#endif

namespace TnTRFMod.Scenes.Enso;

public class HitStatusPanel
{
    private EnsoGameManager _ensoGameManager;
    private int curFuka;
    private int curKa;
    private int curRenda;

    private int curRyo;
    private TextUi fukaAspect;
    private TextUi fukaCounter;

    private TextUi hitAspectValue;

    private TextUi kaAspect;
    private TextUi kaCounter;

    private TextUi rendaCounter;

    private TextUi ryoAspect;

    private TextUi ryoCounter;
    private ImageUi trainCounterUi;

    public void Start()
    {
        _ensoGameManager = GameObject.Find("EnsoGameManager").GetComponent<EnsoGameManager>();

        trainCounterUi = new ImageUi(TextureManager.Textures.TrainCounter)
        {
            Position = new Vector2(37.5f, 577.5f),
            Name = "TrainCounterSprite"
        };

        var hitAspectLabel = new TextUi(true)
        {
            I18nText = I18n.Get("hitStats.hitAspectLabel"),
            FontSize = 50
        };
        trainCounterUi.AddChild(hitAspectLabel);
        hitAspectLabel.Position = new Vector2(33, 40);

        var aspectTextColor = new Color32(219, 150, 0, 255);

        var rendaLabel = new TextUi(true)
        {
            I18nText = I18n.Get("hitStats.rendaLabel"),
            FontSize = 40,
            Color = new Color32(255, 255, 35, 255)
        };
        trainCounterUi.AddChild(rendaLabel);
        rendaLabel.Position = new Vector2(51, 338);

        const int LabelAlignX = 130;

        var ryo = new ImageUi(TextureManager.Textures.HitRyo);
        trainCounterUi.AddChild(ryo);
        ryo.Position = new Vector2(LabelAlignX - ryo.Size.x, 121);
        var ka = new ImageUi(TextureManager.Textures.HitKa);
        trainCounterUi.AddChild(ka);
        ka.Position = new Vector2(LabelAlignX - ka.Size.x, 179);
        var fuka = new ImageUi(TextureManager.Textures.HitFuka);
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
    }

    private void UpdatePanel()
    {
        var ryo = EnsoGameBasePatch.PlayerStates[0].RyoCount;
        var ka = EnsoGameBasePatch.PlayerStates[0].KaCount;
        var fuka = EnsoGameBasePatch.PlayerStates[0].FuKaCount;
        var renda = EnsoGameBasePatch.PlayerStates[0].RendaCount;
        // 如果没有变化则不更新
        if (ryo == curRyo && ka == curKa && fuka == curFuka && renda == curRenda) return;
        curRyo = ryo;
        curKa = ka;
        curFuka = fuka;
        curRenda = renda;

        var total = ryo + ka + fuka;

        // ryoCounter.Text = ryo.ToString();
        // kaCounter.Text = ka.ToString();
        // fukaCounter.Text = fuka.ToString();
        ryoCounter.SetText("{0}", ryo);
        kaCounter.SetText("{0}", ka);
        fukaCounter.SetText("{0}", fuka);

        if (total > 0)
        {
            var totalRyoAndKa = ryo + ka;
            if (fuka == 0)
            {
                var ryoAspectValue = (int)(100 * (ryo / (float)totalRyoAndKa));
                // ryoAspect.Text = $"{ryoAspectValue}%";
                // kaAspect.Text = $"{100 - ryoAspectValue}%";

                ryoAspect.SetText("{0}%", ryoAspectValue);
                kaAspect.SetText("{0}%", 100 - ryoAspectValue);
                fukaAspect.Text = "0%";
                hitAspectValue.Text = "100%";
            }
            else
            {
                var ryoAspectValue = (int)(100 * (ryo / (float)total));
                var kaAspectValue = (int)(100 * (ka / (float)total));
                var fukaAspectValue = 100 - ryoAspectValue - kaAspectValue;
                // ryoAspect.Text = $"{ryoAspectValue}%";
                // kaAspect.Text = $"{kaAspectValue}%";
                // fukaAspect.Text = $"{fukaAspectValue}%";
                // hitAspectValue.Text = $"{100 - fukaAspectValue}%";

                ryoAspect.SetText("{0}%", ryoAspectValue);
                kaAspect.SetText("{0}%", kaAspectValue);
                fukaAspect.SetText("{0}%", fukaAspectValue);
                hitAspectValue.SetText("{0}%", 100 - fukaAspectValue);
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

        // rendaCounter.Text = EnsoGameBasePatch.RendaCount.ToString();
        rendaCounter.SetText("{0}", renda);
    }

    public void Update()
    {
        UpdatePanel();
    }
}