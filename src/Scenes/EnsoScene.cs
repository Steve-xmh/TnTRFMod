using TnTRFMod.Patches;

namespace TnTRFMod.Ui.Scenes;

public class EnsoScene : IScene
{
    public string SceneName => "Enso";

    public void Start()
    {
        NoShadowOnpuPatch.CheckOrInitializePatch();
        BufferedNoteInputPatch.ResetCounts();

        if (TnTrfMod.Instance.enableNearestNeighborOnpuPatch.Value) NearestNeighborOnpuPatch.PatchLaneTarget();
    }
}