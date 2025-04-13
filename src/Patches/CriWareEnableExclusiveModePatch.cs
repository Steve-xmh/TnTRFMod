using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using MinHook;
using TnTRFMod.Utils;
using TnTRFMod.Utils.Wasapi;

namespace TnTRFMod.Patches;

[SuppressMessage("Interoperability", "CA1416")]
public static class CriWareEnableExclusiveModePatch
{
    private const string CriWarePluginName = "Taiko no Tatsujin Rhythm Festival_Data/Plugins/x86_64/cri_ware_unity.dll";
    private static Guid IID_IAudioClient = typeof(IAudioClient3).GUID;
    private static IAudioClient3? audioClient;
    private static IMMDevice? device;
    private static HookEngine? engine;

    private static TimeSpan bufferDuration = TimeSpan.Zero;
    private static bool isFormatSupported;

    private static IAudioClientInitializeHook? AudioClientInitializeHook_Original;

    private static criAtomUnity_Initialize_Delegate? criAtomUnity_Initialize_Original;

    public static void Apply()
    {
        Logger.Info("Starting CriWareEnableExclusiveModePatch");

        IntPtr initializeFuncPtr;
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            var realEnumerator = new MMDeviceEnumeratorComObject() as IMMDeviceEnumerator;
            realEnumerator!.GetDefaultAudioEndpoint(0, 0, out device);

            device.Activate(ref IID_IAudioClient, ClsCtx.ALL, IntPtr.Zero,
                out var audioClient3);
            audioClient = audioClient3 as IAudioClient3;
            if (audioClient == null)
            {
                Logger.Error("Failed to activate IAudioClient3");
                return;
            }

            var comPtr = Marshal.GetComInterfaceForObject(audioClient, typeof(IAudioClient3));
            var vtable = Marshal.ReadIntPtr(comPtr);
            {
                var start = Marshal.GetStartComSlot(typeof(IAudioClient3));
                // int end = Marshal.GetEndComSlot(typeof(IAudioClient3));
                initializeFuncPtr = Marshal.ReadIntPtr(vtable, start * Marshal.SizeOf<IntPtr>());
            }

            audioClient.GetDevicePeriod(out _, out var period);
            bufferDuration = new TimeSpan(period);
            // audioClient.GetBufferSize(out var bufferFrameCount);
            // bufferDuration = new TimeSpan((long)(10000.0 * 1000 / format.sampleRate * bufferFrameCount + 0.5));
            Logger.Info($"Exclusive mode buffer duration: {bufferDuration.TotalMilliseconds}ms");
        }

        criAtom_SetAudioClientShareMode_WASAPI(AudioClientShareMode.Shared); // Load dll

        // var handle = GetModuleHandle("cri_ware_unity.dll");
        // var logCallbackPtr = new IntPtr((long)handle + 0x1802273F0L - 0x180000000L);

        // WriteMemory(logCallbackPtr, Marshal.GetFunctionPointerForDelegate(new OnCriAtomUnityLog(
        //     (buffer, _, info, _) =>
        //     {
        //         var data1 = info.data1.ToString("X8");
        //         var data2 = info.data2.ToString("X8");
        //         var data3 = info.data3.ToString("X8");
        //
        //         Logger.Info(
        //             $"[CriWareUnity] {buffer} ({data1}, {data2}, {data3})");
        //     }
        // )));

        engine = new HookEngine();

        criAtomUnity_Initialize_Original = engine.CreateHook("cri_ware_unity.dll", "CRIWARE2EA3E3EA",
            new criAtomUnity_Initialize_Delegate(criAtomUnity_Initialize_Hook));
        if (initializeFuncPtr != IntPtr.Zero)
            AudioClientInitializeHook_Original = engine.CreateHook(initializeFuncPtr,
                new IAudioClientInitializeHook(AudioClientInitializeHook));

        engine.EnableHooks();
    }

    private static int AudioClientInitializeHook(IntPtr audioclient, AudioClientShareMode sharemode,
        AudioClientStreamFlags streamflags, long hnsbufferduration, long hnsperiodicity, WaveFormat pformat,
        ref Guid audiosessionguid)
    {
        if (isFormatSupported)
            return AudioClientInitializeHook_Original!(audioclient, sharemode,
                streamflags, bufferDuration.Ticks, bufferDuration.Ticks, pformat,
                ref audiosessionguid);
        return AudioClientInitializeHook_Original!(audioclient, sharemode,
            streamflags, hnsbufferduration, hnsperiodicity, pformat,
            ref audiosessionguid);
    }

    private static void WriteMemory<T>(IntPtr location, [DisallowNull] T value)
    {
        var size = Marshal.SizeOf<T>();
        var buffer = Marshal.AllocHGlobal(size);
        VirtualProtect(location, size, 0x40, out var oldProect);
        Marshal.StructureToPtr(value, buffer, false);
        var bytes = new byte[size];
        Marshal.Copy(buffer, bytes, 0, size);
        Marshal.FreeHGlobal(buffer);
        Marshal.Copy(bytes, 0, location, size);
        VirtualProtect(location, size, oldProect, out _);
    }

    private static WaveFormat GetWaveFormat()
    {
        var format = new WaveFormat
        {
            waveFormatTag = WaveFormatEncoding.Pcm,
            channels = TnTrfMod.Instance.exclusiveModeAudioChannels.Value,
            sampleRate = TnTrfMod.Instance.exclusiveModeAudioSampleRate.Value,
            bitsPerSample = TnTrfMod.Instance.exclusiveModeAudioBitsPerSample.Value,
            extraSize = 0
        };
        format.blockAlign = (short)(format.bitsPerSample / 8 * format.channels);
        format.averageBytesPerSecond = format.sampleRate * format.blockAlign;
        return format;
    }

    private static long criAtomUnity_Initialize_Hook()
    {
        // Logger.Info("Called hooked criAtomUnity_Initialize, trying to initialize with exclusive mode");

        if (bufferDuration != TimeSpan.Zero)
        {
            var format = GetWaveFormat();
            var formatPtr = Marshal.AllocHGlobal(Marshal.SizeOf(format));
            Marshal.StructureToPtr(format, formatPtr, false);

            isFormatSupported = criAtom_GetAudioClientIsFormatSupported_WASAPI(formatPtr);
            if (isFormatSupported)
            {
                criAtom_SetAudioClientShareMode_WASAPI(AudioClientShareMode.Exclusive);
                criAtom_SetAudioClientBufferDuration_WASAPI(bufferDuration);
                criAtom_SetAudioClientFormat_WASAPI(formatPtr);
            }
            else
            {
                Logger.Error(
                    "The wave format of the exclusive audio is not supported by the CriWare Unity Plugin, exclusive audio feature is disabled!");
                Logger.Error("\tConfigured wave format:");
                Logger.Error("\t\t- Channels:        " + format.channels);
                Logger.Error("\t\t- Sample Rate:     " + format.sampleRate);
                Logger.Error("\t\t- Bits Per Sample: " + format.bitsPerSample);
            }

            Marshal.FreeHGlobal(formatPtr);
        }

        return criAtomUnity_Initialize_Original!();
    }


    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32", PreserveSig = true)]
    private static extern bool VirtualProtect(IntPtr lpAddress, int dwSize, uint flNewProtect, out uint lpflOldProtect);

    // criAtom_SetAudioClientShareMode_WASAPI
    [DllImport(CriWarePluginName, EntryPoint = "criAtom_SetAudioClientShareMode_WASAPI",
        CallingConvention = CallingConvention.StdCall)]
    private static extern void criAtom_SetAudioClientShareMode_WASAPI(AudioClientShareMode mode);

    [DllImport(CriWarePluginName, EntryPoint = "criAtom_SetAudioClientFormat_WASAPI",
        CallingConvention = CallingConvention.StdCall)]
    private static extern void criAtom_SetAudioClientFormat_WASAPI(IntPtr mode);

    [DllImport(CriWarePluginName, EntryPoint = "criAtom_SetAudioClientFormat_WASAPI",
        CallingConvention = CallingConvention.StdCall)]
    private static extern bool criAtom_GetAudioClientIsFormatSupported_WASAPI(IntPtr mode);

    [DllImport(CriWarePluginName, EntryPoint = "criAtom_SetAudioClientBufferDuration_WASAPI",
        CallingConvention = CallingConvention.StdCall)]
    private static extern void criAtom_SetAudioClientBufferDuration_WASAPI(TimeSpan duration);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 2)]
    private struct LoggingData
    {
        public long data1;
        public long data2;
        public long data3;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int IAudioClientInitializeHook(
        IntPtr audioClient,
        AudioClientShareMode shareMode,
        AudioClientStreamFlags streamFlags,
        long hnsBufferDuration, // REFERENCE_TIME
        long hnsPeriodicity, // REFERENCE_TIME
        [In] WaveFormat pFormat,
        [In] ref Guid audioSessionGuid
    );

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void OnCriAtomUnityLog(string msgBuffer, int level, LoggingData info, IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate long criAtomUnity_Initialize_Delegate();
}