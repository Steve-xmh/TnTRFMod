using HarmonyLib;
using Scripts.OutGame.SongSelect;
using TnTRFMod.Ui;
using TnTRFMod.Ui.Widgets;
using UnityEngine;
using Logger = TnTRFMod.Utils.Logger;

namespace TnTRFMod.Patches;

[HarmonyPatch]
public class ScoreRankIconPatch
{
    private static Texture2D? scoreRankSprites;
    private static readonly List<ImageUi> icons = new(10);

    private static void LoadScoreRankIcons()
    {
        var scoreRankImagePath = Path.Join(TnTrfMod.Dir, "ScoreRank.png");
        byte[] scoreRankImageData;
        if (File.Exists(scoreRankImagePath))
        {
            scoreRankImageData = File.ReadAllBytes(scoreRankImagePath);
            scoreRankSprites = TextureManager.LoadTexture(TextureManager.Textures.ScoreRankIcons, scoreRankImageData);
        }
        else
        {
            Logger.Info($"{scoreRankImagePath} not found, will use builtin alternative.");
            scoreRankSprites = TextureManager.LoadTexture(TextureManager.Textures.ScoreRankIcons);
        }
    }

    public static ImageUi GenerateScoreRankIcon(int scoreRank)
    {
        if (!scoreRankSprites || scoreRankSprites.WasCollected)
            LoadScoreRankIcons();

        var width = scoreRankSprites!.width;
        var heightPerIcon = scoreRankSprites.height / 7;
        var iconSprite = Sprite.Create(scoreRankSprites,
            new Rect(0.0f, (6 - scoreRank) * heightPerIcon, width, heightPerIcon),
            new Vector2(0.5f, 0.5f), width / 140f);
        return new ImageUi(iconSprite);
    }

    [HarmonyPatch(typeof(UiSongCenterButton))]
    [HarmonyPatch(nameof(UiSongCenterButton.Setup))]
    [HarmonyPostfix]
    private static void UiSongCenterButton_Setup_Postfix(UiSongCenterButton __instance,
        ref MusicDataInterface.MusicInfoAccesser item)
    {
        try
        {
            for (var i = 0; i < icons.Count; i++) icons[i].Dispose();
        }
        catch (Exception e)
        {
            Logger.Error($"Error disposing icons: {e}");
        }

        icons.Clear();
        // var modSaveData = CustomSongSaveDataPatch.GetModSaveData(item.UniqueId);
        // if (!modSaveData.HasValue) return;
        // var scoreRanks = modSaveData.Value.scoreRanks;
        // if (scoreRanks == null || scoreRanks.Length == 0) return;
        // for (var i = 0; i < 5; i++)
        // {
        //     var btn = __instance.difficulties[i];
        //     if (!btn) continue;
        //     if (scoreRanks.Length < i) continue;
        //     var scoreRank = scoreRanks[i];
        //     if (scoreRank < 0) continue;
        //     var icon = GenerateScoreRankIcon(scoreRank);
        //     icon.SetParent(btn.gameObject);
        //     icon._transform.localPosition = new Vector3(85, -28, 0);
        //     icon._transform.localScale = Vector3.one;
        //     icon._transform.pivot = new Vector2(0.5f, 0.5f);
        //     icon.Size = new Vector2(52, 52);
        //     icons.Add(icon);
        // }
    }
}