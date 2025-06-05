using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace TnTRFMod.Patches;

internal class NearestNeighborOnpuPatch
{
    // 将判定圈设置成最近邻居
    public static void PatchLaneTarget()
    {
        var laneTarget = GameObject.Find("lane_target");
        if (laneTarget == null) return;
        var laneImage = laneTarget.GetComponentInChildren<Image>();
        laneImage.mainTexture.filterMode = FilterMode.Point;
    }

    [HarmonyPatch(typeof(SpriteAnimation))]
    [HarmonyPatch(nameof(SpriteAnimation.ChangeAnimation))]
    [HarmonyPatch([typeof(AnimationData), typeof(float)])]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPrefix]
    private static void SpriteAnimation_ChangeAnimation_Postfix(SpriteAnimation __instance, ref AnimationData data,
        ref float time)
    {
        if (!TnTrfMod.Instance.enableNearestNeighborOnpuPatch.Value) return;
        for (var i = 0; i < data.spriteList.Count; i++)
            data.spriteList._items[i].sprite.texture.filterMode = FilterMode.Point;
    }
}