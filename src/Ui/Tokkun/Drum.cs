using TnTRFMod.Ui.Widgets;
using UnityEngine;

namespace TnTRFMod.Ui.Tokkun;

public class Drum : BaseUi
{
    private const float DefaultButtonScale = 0.75f;
    private const float HitButtonScale = 0.9f;
    private const float ScaleDecaySpeed = 2f;
    private const float FadeSpeed = 5f;

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

    // Cached mutable state to avoid per-frame allocations
    private Color _donEffectColor = new(1f, 1f, 1f, 0f);
    private Color _leftEffectColor = new(1f, 1f, 1f, 0f);
    private Color _rightEffectColor = new(1f, 1f, 1f, 0f);
    private Vector3 _donButtonScale = new(DefaultButtonScale, DefaultButtonScale, DefaultButtonScale);
    private Vector3 _leftButtonScale = new(DefaultButtonScale, DefaultButtonScale, DefaultButtonScale);
    private Vector3 _rightButtonScale = new(DefaultButtonScale, DefaultButtonScale, DefaultButtonScale);

    // Layout positions (design-time 1920x1080 coordinates)
    private static readonly Vector2 HitEffectDonPos = new(71f, 64f);
    private static readonly Vector2 HitEffectLeftKatsuPos = new(-10f, -18f);
    private static readonly Vector2 HitEffectRightKatsuPos = new(298f, -18f);
    private static readonly Vector2 ButtonDonPos = new(317f, 270f);
    private static readonly Vector2 ButtonLeftKatsuPos = new(0f, 130f);
    private static readonly Vector2 ButtonRightKatsuPos = new(634f, 130f);

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

        drumHitEffectDonImage.Position = HitEffectDonPos;
        drumHitEffectLeftKatsuImage.Position = HitEffectLeftKatsuPos;
        drumHitEffectRightKatsuImage.Position = HitEffectRightKatsuPos;

        drumHitEffectLeftKatsuImage.Image.color = _leftEffectColor;
        drumHitEffectRightKatsuImage.Image.color = _rightEffectColor;
        drumHitEffectDonImage.Image.color = _donEffectColor;

        drumButtonDon.Position = ButtonDonPos;
        drumButtonLeftKatsu.Position = ButtonLeftKatsuPos;
        drumButtonRightKatsu.Position = ButtonRightKatsuPos;

        drumButtonDon._transform.localScale = _donButtonScale;
        drumButtonLeftKatsu._transform.localScale = _leftButtonScale;
        drumButtonRightKatsu._transform.localScale = _rightButtonScale;

        _transform.localScale = new Vector3(DefaultButtonScale, DefaultButtonScale, DefaultButtonScale);

        drumButtonDon.SetActionIcon(LoadImage(Resources.TokkunIconPause));
    }

    public void Update()
    {
        var dt = Time.deltaTime;

        _donEffectColor.a = Math.Max(0f, _donEffectColor.a - dt * FadeSpeed);
        drumHitEffectDonImage.Image.color = _donEffectColor;
        _leftEffectColor.a = Math.Max(0f, _leftEffectColor.a - dt * FadeSpeed);
        drumHitEffectLeftKatsuImage.Image.color = _leftEffectColor;
        _rightEffectColor.a = Math.Max(0f, _rightEffectColor.a - dt * FadeSpeed);
        drumHitEffectRightKatsuImage.Image.color = _rightEffectColor;

        var ss = dt * ScaleDecaySpeed;
        _donButtonScale.x =
            _donButtonScale.y = _donButtonScale.z = Math.Max(DefaultButtonScale, _donButtonScale.x - ss);
        drumButtonDon._transform.localScale = _donButtonScale;
        _leftButtonScale.x = _leftButtonScale.y =
            _leftButtonScale.z = Math.Max(DefaultButtonScale, _leftButtonScale.x - ss);
        drumButtonLeftKatsu._transform.localScale = _leftButtonScale;
        _rightButtonScale.x = _rightButtonScale.y =
            _rightButtonScale.z = Math.Max(DefaultButtonScale, _rightButtonScale.x - ss);
        drumButtonRightKatsu._transform.localScale = _rightButtonScale;
    }


    public void InvokeDon()
    {
        _donEffectColor.a = 1f;
        drumHitEffectDonImage.Image.color = _donEffectColor;
        _donButtonScale.x = _donButtonScale.y = _donButtonScale.z = HitButtonScale;
        drumButtonDon._transform.localScale = _donButtonScale;
    }

    public void InvokeLeftKatsu()
    {
        _leftEffectColor.a = 1f;
        drumHitEffectLeftKatsuImage.Image.color = _leftEffectColor;
        _leftButtonScale.x = _leftButtonScale.y = _leftButtonScale.z = HitButtonScale;
        drumButtonLeftKatsu._transform.localScale = _leftButtonScale;
    }

    public void InvokeRightKatsu()
    {
        _rightEffectColor.a = 1f;
        drumHitEffectRightKatsuImage.Image.color = _rightEffectColor;
        _rightButtonScale.x = _rightButtonScale.y = _rightButtonScale.z = HitButtonScale;
        drumButtonRightKatsu._transform.localScale = _rightButtonScale;
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