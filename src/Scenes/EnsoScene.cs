using System.Collections;
using TnTRFMod.Patches;
using TnTRFMod.Scenes.Enso;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace TnTRFMod.Scenes;

public class EnsoScene : IScene
{
    private readonly HitOffsetTip HitOffsetTip = new();
    private readonly HitStatusPanel HitStatusPanel = new();
    private readonly ScoreRankIcon ScoreRankIcon = new();

    public string SceneName => "Enso";

    public void Start()
    {
        NoShadowOnpuPatch.CheckOrInitializePatch();
        BufferedNoteInputPatch.ResetCounts();

        GarbageCollector.SetMode(GarbageCollector.Mode.Disabled);

        LiveStreamSongSelectPanel.QueuedSongList.Remove(LiveStreamSongSelectPanel.QueuedSongList.Find(info =>
            info.SongInfo.UniqueId == CommonObjects.Instance.MyDataManager.EnsoData.ensoSettings.musicUniqueId));

        if (TnTrfMod.Instance.enableNearestNeighborOnpuPatch.Value) NearestNeighborOnpuPatch.PatchLaneTarget();
        if (TnTrfMod.Instance.enableHitStatsPanelPatch.Value) HitStatusPanel.Start();
        if (TnTrfMod.Instance.enableHitOffset.Value) HitOffsetTip.Start();

        if (TnTrfMod.Instance.enableScoreRankIcon.Value) ScoreRankIcon.Init();
        if (TnTrfMod.Instance.enableOnpuTextRail.Value) TnTrfMod.Instance.StartCoroutine(DrawOnpuTextRail());
    }

    public void Destroy()
    {
        GarbageCollector.SetMode(GarbageCollector.Mode.Enabled);
    }

    public void Update()
    {
        if (TnTrfMod.Instance.enableHitStatsPanelPatch.Value) HitStatusPanel.Update();
        if (TnTrfMod.Instance.enableScoreRankIcon.Value) ScoreRankIcon.Update();
        if (TnTrfMod.Instance.enableHitOffset.Value) HitOffsetTip.Update();
    }

    private IEnumerator DrawOnpuTextRail()
    {
        GameObject lane = null;
        while (!lane)
        {
            lane = GameObject.Find("CanvasBack/lane");
            yield return null;
        }

        var textLaneTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        textLaneTexture.SetPixel(0, 0, new Color32(132, 132, 132, 255));
        textLaneTexture.Apply();
        var textLaneOutlineTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        textLaneOutlineTexture.SetPixel(0, 0, Color.black);
        textLaneOutlineTexture.Apply();
        var textLane = new GameObject("text_lane");
        var textLaneTransform = textLane.AddComponent<RectTransform>();
        var textLaneImage = textLane.AddComponent<Image>();
        textLaneTransform.SetParent(lane.transform);
        textLaneTransform.sizeDelta = new Vector2(1428, 53);
        textLaneTransform.localPosition = new Vector3(246, -93.5f, 0);
        textLaneImage.sprite = Sprite.Create(textLaneTexture,
            new Rect(0, 0, textLaneTexture.width, textLaneTexture.height), Vector2.zero);
        var textLaneOutline = new GameObject("text_lane_outline");
        var textLaneOutlineTransform = textLaneOutline.AddComponent<RectTransform>();
        var textLaneOutlineImage = textLaneOutline.AddComponent<Image>();
        textLaneOutlineTransform.SetParent(lane.transform);
        textLaneOutlineTransform.sizeDelta = new Vector2(1428, 7);
        textLaneOutlineTransform.localPosition = new Vector3(246, -65, 0);
        textLaneOutlineImage.sprite = Sprite.Create(textLaneOutlineTexture,
            new Rect(0, 0, textLaneOutlineTexture.width, textLaneOutlineTexture.height), Vector2.zero);
    }
}