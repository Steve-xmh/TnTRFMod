using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppWebSocketSharp;
using MelonLoader;
using MethodType = HarmonyLib.MethodType;

namespace TnTRFMod.Patches;

[HarmonyPatch]
internal class SongNamePatch
{
    [HarmonyPatch(typeof(MusicDataInterface))]
    [HarmonyPatch(nameof(MusicDataInterface.AddMusicInfo))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPrefix]
    private static void MusicDataInterface_AddMusicInfo_Prefix(ref MusicDataInterface.MusicInfo musicinfo)
    {
        if (musicinfo.SongNameCN.IsNullOrEmpty())
        {
            MelonLogger.Msg($"检测到歌曲 {musicinfo.SongFileName} 没有中文名，已自动设置为日文名");
            musicinfo.SongNameCN = musicinfo.SongNameJP;
        }
    }

    [HarmonyPatch(typeof(MusicDataInterface.MusicInfoAccesser))]
    [HarmonyPatch(nameof(MusicDataInterface.MusicInfoAccesser.SongNames))]
    [HarmonyPatch(MethodType.Getter)]
    [HarmonyPostfix]
    private static void MusicDataInterface_MusicInfoAccesser_SongNames_Getter_Postfix(
        ref MusicDataInterface.MusicInfoAccesser __instance, ref Il2CppStringArray __result)
    {
        if (__result.Length == 0)
        {
            MelonLogger.Msg($"检测到歌曲 {__instance.SongFileName} 的歌曲名称为空，已自动设置为日文名");
            __instance.IsDispJpSongName = true;
        }
    }
}