using Il2Cpp;
using TnTRFMod.Ui.Widgets;
using UnityEngine;

namespace TnTRFMod.Ui.Scenes;

public class DressUpModScene
{
    public static void Setup()
    {
        var switchAnimationBtn = new ButtonUi
        {
            Text = "切换动画",
            Position = new Vector2(32f, 32f)
        };
        var scroll = new ScrollContainerUi
        {
            Position = new Vector2(32f, 64f),
            Size = new Vector2(512f, 300f),
            Visible = false
        };
        switchAnimationBtn.AddListener(() => { scroll.Visible = !scroll.Visible; });
        foreach (var entry in DonModelAnimationEntry.DonModelAnimationEnties)
        {
            var button = new ButtonUi
            {
                Position = new Vector2(0, 0),
                Size = new Vector2(256f, 32f),
                Text = entry.Name
            };
            button.AddListener(() =>
            {
                var donModel = DonModel.GetInstance(0);
                donModel.PlayAnimation(DonModelAnimationDefine.Animations.Normal);
                donModel.PlayAnimation(entry.Animation);
            });
            scroll.AddChild(button);
        }
    }

    public static void OnUpdate()
    {
        ControllerManager.GetKeyboard(out var keyboard);

        if (keyboard.yKey.isPressed)
        {
            var donModel = DonModel.GetInstance(0);
            donModel._rootModels.transform.Rotate(0, -180 * Time.deltaTime, 0);
        }
        else if (keyboard.tKey.isPressed)
        {
            var donModel = DonModel.GetInstance(0);
            donModel._rootModels.transform.Rotate(0, 180 * Time.deltaTime, 0);
        }
    }

    public static void OnUnload()
    {
        var donModel = DonModel.GetInstance(0);
        donModel._rootModels.transform.rotation = Quaternion.Euler(0, 180, 0);
    }

    private struct DonModelAnimationEntry
    {
        public static readonly DonModelAnimationEntry[] DonModelAnimationEnties =
        [
            new("Balloon_Failure", DonModelAnimationDefine.Animations.Balloon_Failure),
            new("Balloon_Loop", DonModelAnimationDefine.Animations.Balloon_Loop),
            new("Balloon_Nobeat", DonModelAnimationDefine.Animations.Balloon_Nobeat),
            new("Balloon_Success", DonModelAnimationDefine.Animations.Balloon_Success),
            new("Combo", DonModelAnimationDefine.Animations.Combo),
            new("Full_Gauge", DonModelAnimationDefine.Animations.Full_Gauge),
            new("Full_Gauge_Combo", DonModelAnimationDefine.Animations.Full_Gauge_Combo),
            new("Kusu_Failure", DonModelAnimationDefine.Animations.Kusu_Failure),
            new("Kusu_Loop", DonModelAnimationDefine.Animations.Kusu_Loop),
            new("Kusu_Nobeat", DonModelAnimationDefine.Animations.Kusu_Nobeat),
            new("Kusu_Start", DonModelAnimationDefine.Animations.Kusu_Start),
            new("Kusu_Success_01", DonModelAnimationDefine.Animations.Kusu_Success_01),
            new("Kusu_Success_02", DonModelAnimationDefine.Animations.Kusu_Success_02),
            new("Miss", DonModelAnimationDefine.Animations.Miss),
            new("Miss6", DonModelAnimationDefine.Animations.Miss6),
            new("Miss_Normal", DonModelAnimationDefine.Animations.Miss_Normal),
            new("Norm_Down", DonModelAnimationDefine.Animations.Norm_Down),
            new("Norm_Loop", DonModelAnimationDefine.Animations.Norm_Loop),
            new("Norm_Up", DonModelAnimationDefine.Animations.Norm_Up),
            new("Normal", DonModelAnimationDefine.Animations.Normal),
            new("Result_Clear", DonModelAnimationDefine.Animations.Result_Clear),
            new("Result_Clear_Loop", DonModelAnimationDefine.Animations.Result_Clear_Loop),
            new("Result_Fullcombo", DonModelAnimationDefine.Animations.Result_Fullcombo),
            new("Result_Fullcombo_Loop", DonModelAnimationDefine.Animations.Result_Fullcombo_Loop),
            new("Result_In", DonModelAnimationDefine.Animations.Result_In),
            new("Result_Loop", DonModelAnimationDefine.Animations.Result_Loop),
            new("Result_Miss", DonModelAnimationDefine.Animations.Result_Miss),
            new("Result_Miss_Loop", DonModelAnimationDefine.Animations.Result_Miss_Loop),
            new("Result_Percious", DonModelAnimationDefine.Animations.Result_Percious),
            new("Result_Percious_Loop", DonModelAnimationDefine.Animations.Result_Percious_Loop),
            new("Sabi", DonModelAnimationDefine.Animations.Sabi),
            new("Sabi_Start", DonModelAnimationDefine.Animations.Sabi_Start),
            new("Select", DonModelAnimationDefine.Animations.Select),
            new("Select_Loop", DonModelAnimationDefine.Animations.Select_Loop),
            new("Wait", DonModelAnimationDefine.Animations.Wait),
            new("Emote_01", DonModelAnimationDefine.Animations.Emote_01),
            new("Emote_02", DonModelAnimationDefine.Animations.Emote_02),
            new("Emote_03", DonModelAnimationDefine.Animations.Emote_03),
            new("Emote_04", DonModelAnimationDefine.Animations.Emote_04),
            new("Emote_05", DonModelAnimationDefine.Animations.Emote_05),
            new("Ninja_Select", DonModelAnimationDefine.Animations.Ninja_Select),
            new("Kusu_Start_Ex", DonModelAnimationDefine.Animations.Kusu_Start_Ex),
            new("Select_Loop_Ex", DonModelAnimationDefine.Animations.Select_Loop_Ex),
            new("Ninja_Attack", DonModelAnimationDefine.Animations.Ninja_Attack),
            new("Ninja_Item", DonModelAnimationDefine.Animations.Ninja_Item),
            new("Ninja_Damage", DonModelAnimationDefine.Animations.Ninja_Damage),
            new("Ninja_Damage02", DonModelAnimationDefine.Animations.Ninja_Damage02),
            new("Ninja_Normal", DonModelAnimationDefine.Animations.Ninja_Normal)
        ];

        public DonModelAnimationEntry(string name, DonModelAnimationDefine.Animations animation)
        {
            Name = name;
            Animation = animation;
        }

        public string Name { get; }
        public DonModelAnimationDefine.Animations Animation { get; }
    }
}