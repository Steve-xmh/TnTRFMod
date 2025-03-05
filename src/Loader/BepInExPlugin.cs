#if BEPINEX
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Logger = TnTRFMod.Utils.Logger;

// ReSharper disable ClassNeverInstantiated.Global

namespace TnTRFMod.Loader;

[BepInPlugin(TnTrfMod.MOD_GUID, TnTrfMod.MOD_NAME, TnTrfMod.MOD_VERSION)]
public class BepInExPlugin : BasePlugin
{
    public static BepInExPlugin Instance { get; private set; }

    public override void Load()
    {
        Instance = this;
        Logger._inner = Log;
        TnTrfMod.Instance = new TnTrfMod
        {
            _updater = AddComponent<TnTrfMod.Updater>()
        };
        TnTrfMod.Instance.Load(new Harmony(TnTrfMod.MOD_GUID));
    }
}

#endif