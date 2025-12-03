using System.Collections;
using TnTRFMod.Patches;
using UnityEngine;

namespace TnTRFMod.Scenes.Enso;

public class ScoreRankIcon
{
    private EnsoGameManager _ensoGameManager;

    private int[] lastScoreRanks;

    public void Init()
    {
        EnsoGameBasePatch.ResetCounts();
        _ensoGameManager = GameObject.Find("EnsoGameManager").GetComponent<EnsoGameManager>();
        lastScoreRanks = new int[_ensoGameManager.playerNum];
        for (var i = 0; i < _ensoGameManager.playerNum; i++)
            lastScoreRanks[i] = -1;
    }

    public void Update()
    {
        if (!EnsoGameBasePatch.IsShinuchiMode) return; // 如果不是真打模式则禁用
        if (!EnsoGameBasePatch.IsPlaying) return; // 如果不是真打模式则禁用

        if (_ensoGameManager.fumenLoader.playerData == null ||
            _ensoGameManager.fumenLoader.playerData.Count == 0) return;

        // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
        var lastScoreRank = lastScoreRanks[0]; // TODO: 支持多玩家
        var curScoreRank = EnsoGameBasePatch.PlayerStates[0].CurrentScoreRank;
        if (lastScoreRank != curScoreRank)
        {
            lastScoreRanks[0] = curScoreRank;
            if (curScoreRank >= 0 && lastScoreRank < curScoreRank)
                TnTrfMod.Instance.StartCoroutine(ShowScoreRank(curScoreRank));
        }
    }

    private IEnumerator ShowScoreRank(int level)
    {
        var iconUi = ScoreRankIconPatch.GenerateScoreRankIcon(level);

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

        iconUi._transform.localScale = Vector3.one * 1.05f;
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
}