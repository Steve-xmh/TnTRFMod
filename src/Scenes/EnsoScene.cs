using TnTRFMod.Patches;
using TnTRFMod.Scenes.Enso;

namespace TnTRFMod.Scenes;

public class EnsoScene : IScene
{
    private readonly HitStatusPanel HitStatusPanel = new();

    public string SceneName => "Enso";

    public void Init()
    {
    }

    public void Start()
    {
        NoShadowOnpuPatch.CheckOrInitializePatch();
        BufferedNoteInputPatch.ResetCounts();

        LiveStreamSongSelectPanel.QueuedSongList.Remove(LiveStreamSongSelectPanel.QueuedSongList.Find(info =>
            info.SongInfo.UniqueId == CommonObjects.Instance.MyDataManager.EnsoData.ensoSettings.musicUniqueId));

        if (TnTrfMod.Instance.enableNearestNeighborOnpuPatch.Value) NearestNeighborOnpuPatch.PatchLaneTarget();
        if (TnTrfMod.Instance.enableHitStatsPanelPatch.Value) HitStatusPanel.StartHitStatsPanel();
    }

    public void Update()
    {
        if (TnTrfMod.Instance.enableHitStatsPanelPatch.Value) HitStatusPanel.UpdateHitStatsPanel();
    }
}