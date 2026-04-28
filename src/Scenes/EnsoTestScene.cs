using TnTRFMod.Config;
using TnTRFMod.Patches;
using TnTRFMod.Scenes.Enso;

namespace TnTRFMod.Scenes;

public class EnsoTestScene : IScene
{
    private readonly HitOffsetTip HitOffsetTip = new();
    private readonly HitStatusPanel HitStatusPanel = new();

    public string SceneName => "EnsoTest";
    public bool LowLatencyMode => true;

    public void Init()
    {
    }

    public void Start()
    {
        NoShadowOnpuPatch.CheckOrInitializePatch();
        BufferedNoteInputPatch.ResetCounts();

        if (ModConfig.EnableNearestNeighborOnpuPatch.Value) NearestNeighborOnpuPatch.PatchLaneTarget();
        if (ModConfig.EnableHitStatsPanelPatch.Value) HitStatusPanel.Start();
        if (ModConfig.EnableHitOffset.Value) HitOffsetTip.Start();
    }

    public void Update()
    {
        if (ModConfig.EnableHitStatsPanelPatch.Value) HitStatusPanel.Update();
        if (ModConfig.EnableHitOffset.Value) HitOffsetTip.Update();
    }
}