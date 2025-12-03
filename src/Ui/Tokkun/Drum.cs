using TnTRFMod.Ui.Widgets;
using UnityEngine;

namespace TnTRFMod.Ui.Tokkun;

public class Drum : BaseUi
{
    public enum Action
    {
        None,
        Pause,
        Resume,
        Rewind,
        Forward,

        SlowPlayback,
        FastPlayback,

        Max
    }

    private readonly DrumButton drumButtonDon;
    private readonly DrumButton drumButtonLeftKatsu;
    private readonly DrumButton drumButtonRightKatsu;
    private readonly ImageUi drumHitEffectDonImage;
    private readonly ImageUi drumHitEffectLeftKatsuImage;
    private readonly ImageUi drumHitEffectRightKatsuImage;
    private readonly ImageUi drumImage;

    private readonly Sprite?[] iconSprites = new Sprite[4];

    public Drum()
    {
        Name = "TokkunDrum";
        drumImage = new ImageUi(TextureManager.Textures.TokkunDrum)
        {
            Name = "DrumImage"
        };
        drumHitEffectDonImage = new ImageUi(TextureManager.Textures.TokkunDrumHitEffectDon)
        {
            Name = "DrumHitEffectDonImage"
        };
        drumHitEffectLeftKatsuImage = new ImageUi(TextureManager.Textures.TokkunDrumHitEffectLeftKatsu)
        {
            Name = "DrumHitEffectLeftKatsuImage"
        };
        drumHitEffectRightKatsuImage = new ImageUi(TextureManager.Textures.TokkunDrumHitEffectRightKatsu)
        {
            Name = "DrumHitEffectRightKatsuImage"
        };
        drumButtonDon = new DrumButton();
        drumButtonLeftKatsu = new DrumButton(true);
        drumButtonRightKatsu = new DrumButton(true);

        AddChild(drumImage);
        AddChild(drumHitEffectLeftKatsuImage);
        AddChild(drumHitEffectRightKatsuImage);
        AddChild(drumHitEffectDonImage);

        AddChild(drumButtonDon);
        AddChild(drumButtonLeftKatsu);
        AddChild(drumButtonRightKatsu);

        drumButtonDon._transform.pivot = new Vector2(1f, 0f);
        drumButtonLeftKatsu._transform.pivot = new Vector2(1f, 0f);
        drumButtonRightKatsu._transform.pivot = new Vector2(1f, 0f);

        drumHitEffectDonImage.Position = new Vector2(71f, 64f);
        drumHitEffectLeftKatsuImage.Position = new Vector2(-10f, -18f);
        drumHitEffectRightKatsuImage.Position = new Vector2(-10f + 308f, -18f);

        drumHitEffectLeftKatsuImage.Image.color = Color.white.AlphaMultiplied(0f);
        drumHitEffectRightKatsuImage.Image.color = Color.white.AlphaMultiplied(0f);
        drumHitEffectDonImage.Image.color = Color.white.AlphaMultiplied(0f);

        drumButtonDon.Position = new Vector2(217f + 100f, 140f + 130f);
        drumButtonLeftKatsu.Position = new Vector2(-100f + 100f, 0f + 130f);
        drumButtonRightKatsu.Position = new Vector2(634f - 100f + 100f, 0f + 130f);

        drumButtonDon._transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
        drumButtonLeftKatsu._transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
        drumButtonRightKatsu._transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);

        _transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);

        drumButtonDon.SetActionIcon(LoadImage(Resources.TokkunIconPause));
    }

    public void Update()
    {
        drumHitEffectDonImage.Image.color =
            Color.white.AlphaMultiplied(Math.Max(0f, drumHitEffectDonImage.Image.color.a - Time.deltaTime * 5f));
        drumHitEffectLeftKatsuImage.Image.color =
            Color.white.AlphaMultiplied(Math.Max(0f, drumHitEffectLeftKatsuImage.Image.color.a - Time.deltaTime * 5f));
        drumHitEffectRightKatsuImage.Image.color =
            Color.white.AlphaMultiplied(Math.Max(0f, drumHitEffectRightKatsuImage.Image.color.a - Time.deltaTime * 5f));
        drumButtonDon._transform.localScale =
            Scale(Math.Max(0.75f, drumButtonDon._transform.localScale.x - Time.deltaTime * 2f));
        drumButtonLeftKatsu._transform.localScale =
            Scale(Math.Max(0.75f, drumButtonLeftKatsu._transform.localScale.x - Time.deltaTime * 2f));
        drumButtonRightKatsu._transform.localScale =
            Scale(Math.Max(0.75f, drumButtonRightKatsu._transform.localScale.x - Time.deltaTime * 2f));
    }

    private static Vector3 Scale(float v)
    {
        return new Vector3(v, v, v);
    }

    public void InvokeDon()
    {
        drumHitEffectDonImage.Image.color = Color.white.AlphaMultiplied(1f);
        drumButtonDon._transform.localScale = Scale(0.9f);
    }

    public void InvokeLeftKatsu()
    {
        drumHitEffectLeftKatsuImage.Image.color = Color.white.AlphaMultiplied(1f);
        drumButtonLeftKatsu._transform.localScale = Scale(0.9f);
    }

    public void InvokeRightKatsu()
    {
        drumHitEffectRightKatsuImage.Image.color = Color.white.AlphaMultiplied(1f);
        drumButtonRightKatsu._transform.localScale = Scale(0.9f);
    }

    public void SetDonAction(Action action)
    {
        SetButtonAction(drumButtonDon, action);
    }

    public void SetLeftKatsuAction(Action action)
    {
        SetButtonAction(drumButtonLeftKatsu, action);
    }

    public void SetRightKatsuAction(Action action)
    {
        SetButtonAction(drumButtonRightKatsu, action);
    }

    private void SetButtonAction(DrumButton button, Action action)
    {
        if (action == Action.None)
        {
            if (button.Visible) button.Visible = false;
            return;
        }

        if (!button.Visible) button.Visible = true;
        switch (action)
        {
            case Action.None:
                button.SetLabel("");
                button.SetActionText("");
                break;
            case Action.Pause:
                iconSprites[0] ??= LoadImage(Resources.TokkunIconPause);
                button.SetLabel("暂停");
                button.SetActionIcon(iconSprites[0]);
                break;
            case Action.Resume:
                iconSprites[1] ??= LoadImage(Resources.TokkunIconResume);
                button.SetLabel("播放");
                button.SetActionIcon(iconSprites[1]);
                break;
            case Action.Rewind:
                iconSprites[2] ??= LoadImage(Resources.TokkunIconRewind);
                button.SetLabel("播放位置");
                button.SetActionIcon(iconSprites[2]);
                break;
            case Action.Forward:
                iconSprites[3] ??= LoadImage(Resources.TokkunIconForward);
                button.SetLabel("播放位置");
                button.SetActionIcon(iconSprites[3]);
                break;
            case Action.SlowPlayback:
                button.SetLabel("速度");
                button.SetActionText("慢");
                break;
            case Action.FastPlayback:
                button.SetLabel("速度");
                button.SetActionText("快");
                break;
            case Action.Max:
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }

    private static Sprite LoadImage(byte[] imageData)
    {
        var imageTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        imageTex.LoadImage(imageData);
        var imageSprite =
            Sprite.Create(imageTex, new Rect(0, 0, imageTex.width, imageTex.height), Vector2.zero,
                1f);
        return imageSprite;
    }
}