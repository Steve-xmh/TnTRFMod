#if MELONLOADER
using MelonLoader;
using TnTRFMod.Utils;

// ReSharper disable ClassNeverInstantiated.Global

namespace TnTRFMod.Loader;

public class MelonLoaderMod : MelonMod
{
    public static MelonLoaderMod Instance { get; private set; }
    
    public override void OnInitializeMelon()
    {
        Instance = this;
        Logger._inner = LoggerInstance;
        TnTrfMod.Instance = new TnTrfMod();
        TnTrfMod.Instance.Load(HarmonyInstance);
    }

    public override void OnUpdate()
    {
        TnTrfMod.Instance.OnUpdate();
    }
}
#endif