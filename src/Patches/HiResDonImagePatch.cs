using HarmonyLib;
using UnityEngine;
using Logger = TnTRFMod.Utils.Logger;

namespace TnTRFMod.Patches;

// TODO: 完善功能和配置
[HarmonyPatch]
public class HiResDonImagePatch
{
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(typeof(DonModel), nameof(DonModel.Start))]
    [HarmonyPostfix]
    private static void DonModel_Start_Postfix(ref DonModel __instance)
    {
        __instance.RenderTexture.Release();
        var targetSize = (int)(768 * (Screen.height / 1080f));
        __instance.RenderTexture.width = targetSize;
        __instance.RenderTexture.height = targetSize;
        __instance.RenderTexture.Create();
    }

    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(typeof(DonImage), nameof(DonImage.Initialize))]
    [HarmonyPostfix]
    private static void DonImage_Initialize_Postfix(ref DonImage __instance)
    {
        var count = __instance._donImageMaterial.shader.GetPropertyCount();
        for (var i = 0; i < count; i++)
        {
            var name = __instance._donImageMaterial.shader.GetPropertyName(i);
            Logger.Info($"DonImage_Initialize_Postfix: {name}");
        }
        //
        // __instance._donImageMaterial.SetFloat("_OutLineWidth", 0f);
    }
}