using TnTRFMod.Patches;

namespace TnTRFMod.Scenes;

public class EnsoNetworkScene : IScene
{
    public string SceneName => "EnsoNetwork";

    public void Start()
    {
        NoShadowOnpuPatch.CheckOrInitializePatch();
        BufferedNoteInputPatch.ResetCounts();

        if (TnTrfMod.Instance.enableNearestNeighborOnpuPatch.Value) NearestNeighborOnpuPatch.PatchLaneTarget();
    }
}