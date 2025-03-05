using System.Runtime.InteropServices;
using TnTRFMod.Utils.Wasapi;

namespace TnTRFMod.Utils;

// Refactored from https://github.com/miniant-git/REAL/blob/master/real-app/src/Windows/MinimumLatencyAudioClient.cpp

public class MinimumLatencyAudioClient
{
    private static Guid IID_IAudioClient = typeof(IAudioClient3).GUID;
    private IAudioClient3 audioClient;
    private IMMDevice device;

    public void Start()
    {
        if (Environment.OSVersion.Version.Major < 10)
        {
            Logger.Error("MinimumLatencyAudioClient feature only works on Windows 10 or newer");
            return;
        }

        Logger.Info("Starting MinimumLatencyAudioClient");
        // ReSharper disable once SuspiciousTypeConversion.Global
        var realEnumerator = new MMDeviceEnumeratorComObject() as IMMDeviceEnumerator;

        realEnumerator!.GetDefaultAudioEndpoint(0, 0, out device);
        if (device == null)
        {
            Logger.Error("Failed to get default audio endpoint");
            return;
        }

        device.Activate(ref IID_IAudioClient, ClsCtx.ALL, IntPtr.Zero,
            out var audioClient3);
        audioClient = audioClient3 as IAudioClient3;
        if (audioClient == null)
        {
            Logger.Error("Failed to activate IAudioClient3");
            return;
        }

        audioClient.GetMixFormat(out var waveFormatPtr);
        var waveFormat = WaveFormat.MarshalFromPtr(waveFormatPtr);
        var sampleRate = waveFormat.sampleRate;

        Logger.Info($"MixFormat: {waveFormat}");
        Logger.Info("Device properties:");
        Logger.Info($"      Sample rate          : {sampleRate}hz");

        audioClient.GetSharedModeEnginePeriod(
            waveFormatPtr,
            out var defaultPeriodInFrames,
            out var fundamentalPeriodInFrames,
            out var minPeriodInFrames,
            out var maxPeriodInFrames
        );

        var minLatency = (float)minPeriodInFrames / sampleRate * 1000f;
        var currentLatency = (float)defaultPeriodInFrames / sampleRate * 1000f;

        Logger.Info(
            $"      Buffer size (Min)    : {minLatency.ToString("F2")}ms");
        Logger.Info(
            $"      Buffer size (Default): {currentLatency.ToString("F2")}ms");
        Logger.Info(
            $"      Buffer size (Max)    : {((float)maxPeriodInFrames / sampleRate * 1000f).ToString("F2")}ms");

        audioClient.InitializeSharedAudioStream(0, minPeriodInFrames, waveFormatPtr, IntPtr.Zero);
        Marshal.FreeCoTaskMem(waveFormatPtr);
        audioClient.Start();
        Logger.Info($"Successfully reduced audio latency from {currentLatency}ms -> {minLatency}ms");
    }

    public void Stop()
    {
        Logger.Info("Stopping MinimumLatencyAudioClient");
        audioClient.Stop();
#if WINDOWS
        Marshal.ReleaseComObject(device);
        Marshal.ReleaseComObject(audioClient);
#endif
    }
}