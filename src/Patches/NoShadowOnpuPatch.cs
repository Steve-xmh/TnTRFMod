using System.Text;
using HarmonyLib;
using UnityEngine;

namespace TnTRFMod.Patches;

[HarmonyPatch]
internal class NoShadowOnpuPatch
{
    private static Texture2D _onpuSpriteSetTexture;

    private static Sprite _spriteDon01;
    private static Sprite _spriteDon02;
    private static Sprite _spriteDon03;

    private static Sprite _spriteKatu01;
    private static Sprite _spriteKatu02;
    private static Sprite _spriteKatu03;

    private static Sprite _spriteDonDai01;
    private static Sprite _spriteDonDai02;

    private static Sprite _spriteKatuDai01;
    private static Sprite _spriteKatuDai02;

    private static HashSet<string> _loggedSets = [];

    public static void CheckOrInitializePatch()
    {
        if (_onpuSpriteSetTexture != null) return;

        const int onpuSpriteSetWidth = 1254;
        const int onpuSpriteSetHeight = 298;

        _onpuSpriteSetTexture = new Texture2D(onpuSpriteSetWidth, onpuSpriteSetHeight, TextureFormat.RGBA32, false);
        _onpuSpriteSetTexture.LoadImage(Resources.OnpuSpriteSet);
        _onpuSpriteSetTexture.filterMode = FilterMode.Point;

        const float smallNoteTop = onpuSpriteSetHeight - 120 - 2;
        const float bigNoteTop = 2;

        _spriteDon01 = CreateSprite("don_01_no_shadow", new Rect(2, smallNoteTop, 120, 120));
        _spriteDon02 = CreateSprite("don_02_no_shadow", new Rect(2 + 120 + 2, smallNoteTop, 120, 120));
        _spriteDon03 = CreateSprite("don_03_no_shadow", new Rect(2 + 2 * (120 + 2), smallNoteTop, 120, 120));

        _spriteKatu01 = CreateSprite("katu_01_no_shadow", new Rect(2 + 3 * (120 + 2), smallNoteTop, 120, 120));
        _spriteKatu02 = CreateSprite("katu_02_no_shadow", new Rect(2 + 4 * (120 + 2), smallNoteTop, 120, 120));
        _spriteKatu03 = CreateSprite("katu_03_no_shadow", new Rect(2 + 5 * (120 + 2), smallNoteTop, 120, 120));

        _spriteDonDai01 = CreateSprite("don_dai_01_no_shadow", new Rect(2, bigNoteTop, 172, 172));
        _spriteDonDai02 = CreateSprite("don_dai_02_no_shadow", new Rect(2 + 172 + 2, bigNoteTop, 172, 172));

        _spriteKatuDai01 = CreateSprite("katu_dai_01_no_shadow", new Rect(2 + 2 * (172 + 2), bigNoteTop, 172, 172));
        _spriteKatuDai02 = CreateSprite("katu_dai_02_no_shadow", new Rect(2 + 3 * (172 + 2), bigNoteTop, 172, 172));

        PrintIndices("OnpuNormal.indicesDon", OnpuNormal.indicesDon);
        PrintIndices("OnpuNormal.indicesKatsu", OnpuNormal.indicesKatsu);
        PrintIndices("OnpuNormal.indicesDaiDon", OnpuNormal.indicesDaiDon);
        PrintIndices("OnpuNormal.indicesDaiKatsu", OnpuNormal.indicesDaiKatsu);
    }

    private static void PrintIndices(string name, int[] indices)
    {
        TnTrfMod.Log.LogMessage(name);
        var builder = new StringBuilder();
        builder.Append('[');
        foreach (var index in indices) builder.Append(index).Append(", ");
        builder.Append(']');
        TnTrfMod.Log.LogMessage(builder.ToString());
    }

    private static Sprite CreateSprite(string name, Rect rect)
    {
        var sprite = Sprite.Create(
            _onpuSpriteSetTexture,
            rect,
            new Vector2(0.5f, 0.5f),
            1f,
            0,
            SpriteMeshType.Tight, Vector4.zero);
        sprite.name = name;
        return sprite;
    }

    // [HarmonyPatch(typeof(OnpuNormal))]
    // [HarmonyPatch(MethodType.Constructor)]
    // [HarmonyPatch([typeof(IntPtr)])]
    // [HarmonyPostfix]
    // private static void OnpuNormal_Constructor(OnpuNormal __instance)
    // {
    //     TnTrfMod.Log.LogMessage("OnpuNormal.indicesDon");
    //     TnTrfMod.Log.LogMessage(OnpuNormal.indicesDon);
    //     TnTrfMod.Log.LogMessage("OnpuNormal.indicesKatsu");
    //     TnTrfMod.Log.LogMessage(OnpuNormal.indicesKatsu);
    //     TnTrfMod.Log.LogMessage("OnpuNormal.indicesDaiDon");
    //     TnTrfMod.Log.LogMessage(OnpuNormal.indicesDaiDon);
    //     TnTrfMod.Log.LogMessage("OnpuNormal.indicesDaiKatsu");
    //     TnTrfMod.Log.LogMessage(OnpuNormal.indicesDaiKatsu);
    // }

    [HarmonyPatch(typeof(SpriteAnimation))]
    [HarmonyPatch(nameof(SpriteAnimation.ChangeAnimation))]
    [HarmonyPatch([typeof(AnimationData), typeof(float)])]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPrefix]
    private static void SpriteAnimation_ChangeAnimation_Prefix(SpriteAnimation __instance, ref AnimationData data,
        ref float time)
    {
        if (!TnTrfMod.Instance.enableNoShadowOnpuPatch.Value) return;
        CheckOrInitializePatch();

        // if (_loggedSets.Contains(__instance.spriteAnimationData.name)) return;
        // _loggedSets.Add(__instance.spriteAnimationData.name);
        //
        // TnTrfMod.Log.LogMessage("SpriteAnimation.spriteAnimationData");
        // TnTrfMod.Log.LogMessage($"  name: {__instance.spriteAnimationData.name}");
        // foreach (var animData in __instance.spriteAnimationData.list)
        // {
        //     TnTrfMod.Log.LogMessage($"  sprite: {animData.name}");
        //     foreach (var spriteData in animData.spriteList)
        //     {
        //         TnTrfMod.Log.LogMessage($"    name: {spriteData.spriteName}");
        //         TnTrfMod.Log.LogMessage($"    sprite.GetHashCode: {spriteData.sprite.GetHashCode()}");
        //     }
        // }

        // new OnpuBase().ChangeAnimationOnState(TaikoCoreTypes.OnpuTypes.Do, TaikoCoreTypes.OnpuStateTypes.Active);
        // OnpuNormal.indicesDon

        // foreach (var sprite in data.spriteList)
        // {
        //     var tex = sprite.sprite.texture;
        //     if (collectedTexSets.Contains(tex)) continue;
        //     texSets.Add(tex);
        // collectedTexSets.Add(tex);
        // var textureData = sprite.sprite.texture.EncodeToPNG();
        // sprite.sprite = sprite.sprite.name switch
        // {
        //     "don_01" => _spriteDon01,
        //     "don_02" => _spriteDon02,
        //     "don_03" => _spriteDon03,
        //     "katu_01" => _spriteKatu01,
        //     "katu_02" => _spriteKatu02,
        //     "katu_03" => _spriteKatu03,
        //     "don_dai_01" => _spriteDonDai01,
        //     "don_dai_02" => _spriteDonDai02,
        //     "katu_dai_01" => _spriteKatuDai01,
        //     "katu_dai_02" => _spriteKatuDai02,
        //     _ => sprite.sprite
        // };
        // }

        // foreach (var tex in texSets)
        // {
        //     TnTrfMod.Log.LogMessage(
        //         $"sprite texture name: {tex.name} {tex.format}");
        //     try
        //     {
        //         var pwd = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        //         var dumpTextureDirPath = Path.Join(pwd, "tntrfmod/dump");
        //         var dumpTexturePath = Path.Join(dumpTextureDirPath, $"{tex.name}.png");
        //         TnTrfMod.Log.LogMessage($"dump texture to: {dumpTexturePath}");
        //         if (!File.Exists(dumpTexturePath))
        //         {
        //             var readableTex = new Texture2D(tex.width, tex.height, tex.format, tex.mipmapCount > 1);
        //             Graphics.CopyTexture(tex, readableTex);
        //             var clonedTex = new Texture2D(readableTex.width, readableTex.height, TextureFormat.ARGB32, false);
        //             // copy pixels
        //             clonedTex.SetPixels32(tex.GetPixels32());
        //             var texData = tex.EncodeToPNG();
        //             new Thread(() =>
        //             {
        //                 Directory.CreateDirectory(dumpTextureDirPath);
        //                 File.WriteAllBytes(dumpTexturePath, texData);
        //             }).Start();
        //         }
        //     }
        //     catch (Exception e)
        //     {
        //         TnTrfMod.Log.LogMessage($"Failed to load sprite texture: {e}");
        //     }
        // }
    }
}