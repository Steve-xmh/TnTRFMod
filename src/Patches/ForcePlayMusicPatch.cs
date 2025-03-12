using HarmonyLib;
using UnityEngine;
using Logger = TnTRFMod.Utils.Logger;

namespace TnTRFMod.Patches;

[HarmonyPatch]
public class ForcePlayMusicPatch
{
    [HarmonyPatch(typeof(EnsoSound), nameof(EnsoSound.PlaySong))]
    [HarmonyPostfix]
    private static void EnsoSound_PlaySong_Postfix(EnsoSound __instance)
    {
        Logger.Info("EnsoSound_PlaySong_Postfix");
        // __instance.SetSpeed(0.5f);
        // __instance.songVolume = 1f;
        var ratio = GameObject.Find("FumenLoader").GetComponent<FumenLoader>().settings.trainingSpeedType switch
        {
            EnsoData.TrainingSpeedType.x1_0 => 1f,
            EnsoData.TrainingSpeedType.x0_8 => 0.8f,
            EnsoData.TrainingSpeedType.x0_5 => 0.5f,
            _ => 1f
        };
        var criPlayer = __instance.songPlayer;
        var bgmVolume = CommonObjects.Instance.MyDataManager.EnsoData.ensoSettings.bgmVolume;
        bgmVolume = Math.Clamp(bgmVolume / 0.7673615f, 0f, 1f);
        Logger.Info($"CurSheetName {criPlayer.CueSheetName}");
        // Logger.Info($"Setting ratio to {ratio}");
        Logger.Info($"Setting Volume to {bgmVolume}");
        criPlayer.SetVolume(bgmVolume);
        // criPlayer.Player.SetPitch((float)(1200 * Math.Log2(ratio)));
        // criPlayer.Player.SetPlaybackRatio(ratio);
        // criPlayer.Player.SetDspTimeStretchRatio(1f / ratio);
        // __instance.songPlayer.Player.Update(__instance.songPlayer.Playback);
        // var playback = criPlayer.PlaybackDic[criPlayer.CueSheetName];
        // criPlayer.Player.UpdateAll();
        // Logger.Info($" SongPlayerVolume: {__instance.songPlayer.Volume}");
    }
}