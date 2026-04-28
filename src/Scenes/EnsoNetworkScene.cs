using TnTRFMod.Config;
using TnTRFMod.Patches;

namespace TnTRFMod.Scenes;

public class EnsoNetworkScene : IScene
{
    public string SceneName => "EnsoNetwork";
    public bool LowLatencyMode => true;

    public void Start()
    {
        NoShadowOnpuPatch.CheckOrInitializePatch();
        BufferedNoteInputPatch.ResetCounts();

        if (ModConfig.EnableNearestNeighborOnpuPatch.Value) NearestNeighborOnpuPatch.PatchLaneTarget();
    }
}