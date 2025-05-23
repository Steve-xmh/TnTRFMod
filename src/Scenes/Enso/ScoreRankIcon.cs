using System.Collections;
using TnTRFMod.Ui.Widgets;
using TnTRFMod.Utils.Fumen;
using UnityEngine;
using Logger = TnTRFMod.Utils.Logger;

namespace TnTRFMod.Scenes.Enso;

public class ScoreRankIcon
{
    private EnsoGameManager _ensoGameManager;

    private PlayerStatus[] playerStatuses;

    private Texture2D scoreRankSprites;

    public void Init()
    {
        _ensoGameManager = GameObject.Find("EnsoGameManager").GetComponent<EnsoGameManager>();
        playerStatuses = new PlayerStatus[_ensoGameManager.playerNum];
        for (var i = 0; i < _ensoGameManager.playerNum; i++)
            playerStatuses[i] = new PlayerStatus();
    }

    private void LoadScoreRankIcons()
    {
        var scoreRankImagePath = Path.Join(TnTrfMod.Dir, "ScoreRank.png");
        byte[] scoreRankImageData;
        if (File.Exists(scoreRankImagePath))
        {
            scoreRankImageData = File.ReadAllBytes(scoreRankImagePath);
        }
        else
        {
            Logger.Error($"{scoreRankImagePath} not found, will use builtin alternative.");
            scoreRankImageData = Resources.ScoreRankIcons;
        }

        scoreRankSprites = ImageUi.LoadImage(scoreRankImageData);
    }

    public void Update()
    {
        if (!scoreRankSprites)
        {
            LoadScoreRankIcons();
            if (!scoreRankSprites) return;
        }

        if (_ensoGameManager.fumenLoader.playerData == null ||
            _ensoGameManager.fumenLoader.playerData.Count == 0) return;

        var frameResult =
            _ensoGameManager.ensoParam.GetFrameResults();
        for (var i = 0; i < playerStatuses.Length; i++)
        {
            var score = frameResult.eachPlayer[i].score;
            var playerStatus = playerStatuses[i];
            if (playerStatus.FumenReader is null)
                try
                {
                    var reader = ReadFumen(_ensoGameManager.fumenLoader.playerData[i]);
                    playerStatus.FumenReader = reader;
                    var maxScore = reader.CalculateMaxScore();
                    Logger.Info($"Loaded Fumen score of Player {i + 1}: {maxScore}");
                    playerStatus.LevelInfos =
                    [
                        new LevelInfo
                        {
                            Name = "粹（白）",
                            Score = maxScore * 5 / 10
                        },
                        new LevelInfo
                        {
                            Name = "粹（铜）",
                            Score = maxScore * 6 / 10
                        },
                        new LevelInfo
                        {
                            Name = "粹（银）",
                            Score = maxScore * 7 / 10
                        },
                        new LevelInfo
                        {
                            Name = "雅（金）",
                            Score = maxScore * 8 / 10
                        },
                        new LevelInfo
                        {
                            Name = "雅（粉）",
                            Score = maxScore * 9 / 10
                        },
                        new LevelInfo
                        {
                            Name = "雅（紫）",
                            Score = maxScore * 95 / 100
                        },
                        new LevelInfo
                        {
                            Name = "极",
                            Score = maxScore
                        }
                    ];
                }
                catch (FumenNoLoadedException)
                {
                    continue;
                }

            if (playerStatus.LevelInfos is null) continue;
            if (playerStatus.LevelInfos.Length == 0) continue;
            var nextLevel = playerStatus.NextLevel;
            if (nextLevel >= playerStatus.LevelInfos.Length) continue;
            var nextLevelInfo = playerStatus.LevelInfos[nextLevel];
            if (nextLevelInfo.Score > score) continue;
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            TnTrfMod.Instance.StartCoroutine(ShowScoreRank(nextLevel));
            playerStatus.NextLevel += 1;
        }
    }

    private static FumenReader ReadFumen(FumenLoader.PlayerData playerData)
    {
        var data = playerData.GetFumenDataAsBytes();
        // var fumenName = Path.ChangeExtension(Path.GetFileName(playerData.fumenPath), ".bin");
        // var fumenPath = Path.Combine(TnTrfMod.Dir, fumenName);
        // File.WriteAllBytes(fumenPath, data);
        return new FumenReader(data);
    }

    private IEnumerator ShowScoreRank(int level)
    {
        Logger.Info($"Showing score rank icon {level}");
        var width = scoreRankSprites.width;
        var heightPerIcon = scoreRankSprites.height / 7;
        var iconSprite = Sprite.Create(scoreRankSprites,
            new Rect(0.0f, (6 - level) * heightPerIcon, width, heightPerIcon),
            new Vector2(0.5f, 0.5f), width / 140f);
        var iconUi = new ImageUi(iconSprite);

        var posX = 180;
        var posY = 145;
        iconUi.Image.color = Color.white.AlphaMultiplied(0f);
        iconUi._transform.pivot = new Vector2(0.5f, 0.5f);
        iconUi.Position = new Vector2(posX, posY);
        iconUi.Size = new Vector2(215, 215);

        // tween in 500ms
        var time = 0f;
        while (time < 0.2f)
        {
            iconUi.Position = new Vector2(posX, Math.Max(posY, posY + 20 * (1f - time / 0.2f)));
            iconUi.Image.color = Color.white.AlphaMultiplied(time / 0.2f);
            yield return null;
            time += Time.deltaTime;
        }

        iconUi.Image.color = Color.white;
        iconUi.Position = new Vector2(posX, posY);
        
        time = 0f;
        while (time < 0.05f)
        {
            iconUi._transform.localScale = Vector3.one * (1.05f + time);
            yield return null;
            time += Time.deltaTime;
        }
        
        iconUi._transform.localScale = Vector3.one * (1.05f);
        yield return null;
        
        time = 0f;
        while (time < 0.05f)
        {
            iconUi._transform.localScale = Vector3.one * (1.05f - time);
            yield return null;
            time += Time.deltaTime;
        }
        
        iconUi._transform.localScale = Vector3.one;

        yield return new WaitForSeconds(3);

        time = 0f;
        while (time < 0.2f)
        {
            iconUi.Position = new Vector2(posX, posY - 20 * (time / 0.2f));
            iconUi.Image.color = Color.white.AlphaMultiplied(1f - time / 0.2f);
            yield return null;
            time += Time.deltaTime;
        }
        
        iconUi.Image.color = Color.white.AlphaMultiplied(0f);
        yield return null;
        iconUi.Dispose();
    }

    private struct LevelInfo
    {
        public string Name;
        public int Score;
    }

    private class PlayerStatus
    {
        public int NextLevel = 0;
        public FumenReader? FumenReader = null;
        public LevelInfo[]? LevelInfos = null;
    }
}