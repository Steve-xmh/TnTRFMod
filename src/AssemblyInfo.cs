using System.Reflection;
#if MELONLOADER
using MelonLoader;
using TnTRFMod;
using TnTRFMod.Loader;
#endif

[assembly:
    AssemblyDescription("SteveXMH's TnTRF Mod - A simple mod for PC version of Taiko no Tatsujin: Rhythm Festival.")]

#if MELONLOADER
[assembly: MelonInfo(typeof(MelonLoaderMod), TnTrfMod.MOD_NAME, TnTrfMod.MOD_VERSION, TnTrfMod.MOD_AUTHOR)]
[assembly: MelonColor(255, 0, 127, 255)]
[assembly: HarmonyDontPatchAll]
#endif