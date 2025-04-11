using System.Runtime.InteropServices;
using TnTRFMod.Utils;

namespace TnTRFMod.Patches;

public class HighPrecisionTimerPatch
{
    public static void Apply()
    {
        if (NtQueryTimerResolution(out var currentResolution, out var minimumResolution, out var maximumResolution) ==
            0)
        {
            var setHighResolutionTimer = currentResolution > maximumResolution;

            Logger.Info(
                $"Timer Resolution current: {currentResolution / 10000.0}ms maximum: {maximumResolution / 10000.0}ms");

            if (!setHighResolutionTimer) return;
            if (NtSetTimerResolution(maximumResolution, true, out currentResolution) == 0)
                Logger.Info($"Successfully change Timer resolution to: {currentResolution / 10000.0}ms");
        }
        else
        {
            Logger.Warn("Failed to query current timer resolution");
        }
    }

    [DllImport("ntdll.dll", PreserveSig = true)]
    private static extern int NtSetTimerResolution(ulong desiredResolution, bool setResolution,
        out ulong currentResolution);

    [DllImport("ntdll.dll", PreserveSig = true)]
    private static extern int NtQueryTimerResolution(out ulong currentResolution, out ulong minimumResolution,
        out ulong maximumResolution);
}