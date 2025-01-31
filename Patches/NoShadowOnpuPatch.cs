using HarmonyLib;
using Il2Cpp;
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

    [HarmonyPatch(typeof(SpriteAnimation))]
    [HarmonyPatch(nameof(SpriteAnimation.ChangeAnimation))]
    [HarmonyPatch([typeof(AnimationData), typeof(float)])]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPrefix]
    private static void SpriteAnimation_ChangeAnimation_Postfix(SpriteAnimation __instance, ref AnimationData data,
        ref float time)
    {
        if (TnTrfMod.Instance.enableNoShadowOnpuPatch.Value)
            CheckOrInitializePatch();

        foreach (var sprite in data.spriteList)
        {
            if (TnTrfMod.Instance.enableNearestNeighborOnpuPatch.Value)
                sprite.sprite.texture.filterMode = FilterMode.Point;

            if (TnTrfMod.Instance.enableNoShadowOnpuPatch.Value)
                sprite.sprite = sprite.sprite.name switch
                {
                    "don_01" => _spriteDon01,
                    "don_02" => _spriteDon02,
                    "don_03" => _spriteDon03,
                    "katu_01" => _spriteKatu01,
                    "katu_02" => _spriteKatu02,
                    "katu_03" => _spriteKatu03,
                    "don_dai_01" => _spriteDonDai01,
                    "don_dai_02" => _spriteDonDai02,
                    "katu_dai_01" => _spriteKatuDai01,
                    "katu_dai_02" => _spriteKatuDai02,
                    _ => sprite.sprite
                };
        }
    }
}