using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace TnTRFMod.Patches;

// TODO 将日语歌名替换成中文名称？

[HarmonyPatch]
internal class SongNamePatch
{
    [HarmonyPatch(typeof(MusicDataInterface))]
    [HarmonyPatch(nameof(MusicDataInterface.AddMusicInfo))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPrefix]
    private static void MusicDataInterface_AddMusicInfo_Prefix(ref MusicDataInterface.MusicInfo musicinfo)
    {
    }

    [HarmonyPatch(typeof(MusicDataInterface.MusicInfoAccesser))]
    [HarmonyPatch(nameof(MusicDataInterface.MusicInfoAccesser.SongNames))]
    [HarmonyPatch(MethodType.Getter)]
    [HarmonyPostfix]
    private static void MusicDataInterface_MusicInfoAccesser_SongNames_Getter_Postfix(
        ref MusicDataInterface.MusicInfoAccesser __instance, ref Il2CppStringArray __result)
    {
    }
}