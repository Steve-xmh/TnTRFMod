using NAudio.CoreAudioApi;

namespace TnTRFMod.Utils;

// Refactored from https://github.com/miniant-git/REAL/blob/master/real-app/src/Windows/MinimumLatencyAudioClient.cpp

public class MinimumLatencyAudioClient
{
    private MMDevice device;

    public void Start()
    {
        TnTrfMod.Log.LogInfo("Starting MinimumLatencyAudioClient");
        var enumerator = new MMDeviceEnumerator();
        device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
        var audioClient = device.AudioClient;
        TnTrfMod.Log.LogInfo("Device properties:");
        TnTrfMod.Log.LogInfo($"      Name                 : {device.FriendlyName}");
        TnTrfMod.Log.LogInfo($"      Sample rate          : {audioClient.MixFormat.SampleRate}hz");
        TnTrfMod.Log.LogInfo($"      Buffer size (Min)    : {audioClient.MinimumDevicePeriod / 10000f}ms");
        TnTrfMod.Log.LogInfo($"      Buffer size (Default): {audioClient.DefaultDevicePeriod / 10000f}ms");
        audioClient.Initialize(AudioClientShareMode.Shared, AudioClientStreamFlags.None, 100000,
            0, audioClient.MixFormat, Guid.Empty);
        audioClient.Start();
        TnTrfMod.Log.LogInfo($"      Buffer size (Current): {audioClient.BufferSize}");
        TnTrfMod.Log.LogInfo($"      Latency (Current)    : {audioClient.StreamLatency}ms");
        TnTrfMod.Log.LogInfo("Started MinimumLatencyAudioClient");
    }

    public void Stop()
    {
        TnTrfMod.Log.LogInfo("Stopping MinimumLatencyAudioClient");
        device.AudioClient.Stop();
        device.Dispose();
    }
}