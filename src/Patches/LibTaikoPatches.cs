using System.Runtime.InteropServices;
using HarmonyLib;
using TnTRFMod.Utils;

namespace TnTRFMod.Patches;

[HarmonyPatch]
internal class LibTaikoPatches
{
    [HarmonyPatch(typeof(LibTaikoWrapper))]
    [HarmonyPatch(nameof(LibTaikoWrapper.SetFumen))]
    [HarmonyPrefix]
    private static unsafe void SetFumenPostfix(ref int player, ref void* data, ref int size)
    {
        var buffer = new byte[size];
        Marshal.Copy((IntPtr)data, buffer, 0, size);
        var debugOutputPath = Path.Combine(TnTrfMod.Dir, $"FumenData_P{player + 1}.bin");
        File.WriteAllBytes(debugOutputPath, buffer);
        Logger.Info($"Wrote fumen data to {debugOutputPath}");
    }
}