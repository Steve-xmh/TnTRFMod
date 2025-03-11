using TnTRFMod.Patches;
using TnTRFMod.Scenes.Enso;

namespace TnTRFMod.Scenes;

public class EnsoTestScene : IScene
{
    private readonly HitStatusPanel HitStatusPanel = new();

    public string SceneName => "EnsoTest";

    public void Init()
    {
    }

    public void Start()
    {
        NoShadowOnpuPatch.CheckOrInitializePatch();
        BufferedNoteInputPatch.ResetCounts();

        if (TnTrfMod.Instance.enableNearestNeighborOnpuPatch.Value) NearestNeighborOnpuPatch.PatchLaneTarget();
        if (TnTrfMod.Instance.enableHitStatsPanelPatch.Value) HitStatusPanel.StartHitStatsPanel();
    }

    public void Update()
    {
        if (TnTrfMod.Instance.enableHitStatsPanelPatch.Value) HitStatusPanel.UpdateHitStatsPanel();
    }
}