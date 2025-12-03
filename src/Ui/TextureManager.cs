using UnityEngine;
using UnityEngine.UI;
using Logger = TnTRFMod.Utils.Logger;
using Object = UnityEngine.Object;

namespace TnTRFMod.Ui;

public static class TextureManager
{
    private static int TextureIdCounter = 1;
    private static readonly object TextureIdCounterLock = new();
    private static readonly Il2CppSystem.Collections.Generic.Dictionary<int, Texture2D> Cache = new(32);
    private static GameObject? keeperGameObject;

    private static int AcquireTexId()
    {
        lock (TextureIdCounterLock)
        {
            return TextureIdCounter++;
        }
    }

    public static Texture2D LoadTexture(TexHandle texHandle)
    {
        return LoadTexture(texHandle, null);
    }

    public static Texture2D LoadTexture(TexHandle texHandle, byte[]? overrideImageData)
    {
        if (Cache.TryGetValue(texHandle.Id, out var cachedTex) && cachedTex != null && !cachedTex.WasCollected)
            return cachedTex;
        if (keeperGameObject == null)
        {
            keeperGameObject = new GameObject("TnTRFMod_TextureManagerKeeper");
            Object.DontDestroyOnLoad(keeperGameObject);
        }

        Logger.Info($"Loading texture with id {texHandle.Id}");
        var imageTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        imageTex.LoadImage(overrideImageData ?? texHandle.Data);
        imageTex.filterMode = FilterMode.Trilinear;
        imageTex.name = $"TnTRFMod_Texture_{texHandle.Id}";
        var texObj = new GameObject(imageTex.name);
        texObj.transform.SetParent(keeperGameObject.transform);
        var img = texObj.AddComponent<Image>();
        img.sprite = Sprite.Create(imageTex, new Rect(0, 0, imageTex.width, imageTex.height), Vector2.zero, 1f);
        img.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        Cache[texHandle.Id] = imageTex;
        return imageTex;
    }

    public struct TexHandle(byte[] data)
    {
        public int Id { get; } = AcquireTexId();
        public byte[] Data { get; } = data;
    }

    public static class Textures
    {
        public static readonly TexHandle UiBase = new(Resources.UiBase);
        public static readonly TexHandle OnpuSpriteSet = new(Resources.OnpuSpriteSet);
        public static readonly TexHandle TrainCounter = new(Resources.TrainCounter);
        public static readonly TexHandle HitRyo = new(Resources.HitRyo);
        public static readonly TexHandle HitKa = new(Resources.HitKa);
        public static readonly TexHandle HitFuka = new(Resources.HitFuka);
        public static readonly TexHandle ScoreRankIcons = new(Resources.ScoreRankIcons);
        public static readonly TexHandle TokkunDrum = new(Resources.TokkunDrum);
        public static readonly TexHandle TokkunDrumHitEffectDon = new(Resources.TokkunDrumHitEffectDon);
        public static readonly TexHandle TokkunDrumHitEffectLeftKatsu = new(Resources.TokkunDrumHitEffectLeftKatsu);
        public static readonly TexHandle TokkunDrumHitEffectRightKatsu = new(Resources.TokkunDrumHitEffectRightKatsu);
        public static readonly TexHandle TokkunButtonDon = new(Resources.TokkunButtonDon);
        public static readonly TexHandle TokkunButtonKatsu = new(Resources.TokkunButtonKatsu);
        public static readonly TexHandle TokkunIconLanePaused = new(Resources.TokkunIconLanePaused);
        public static readonly TexHandle TokkunIconForward = new(Resources.TokkunIconForward);
        public static readonly TexHandle TokkunIconPause = new(Resources.TokkunIconPause);
        public static readonly TexHandle TokkunIconResume = new(Resources.TokkunIconResume);
        public static readonly TexHandle TokkunIconRewind = new(Resources.TokkunIconRewind);
    }
}