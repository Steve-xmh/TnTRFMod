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
    private static HookEngine? engine;

    private static WaveFormat? mixFormat;
    private static TimeSpan bufferDuration = TimeSpan.Zero;
    private static bool isFormatSupported;

    private static IAudioClientInitializeHook? AudioClientInitializeHook_Original;

    private static criAtomUnity_Initialize_Delegate? criAtomUnity_Initialize_Original;

    private static
        IntPtr audioClientInitializeFuncPtr = IntPtr.Zero;

    private static bool showedUnsupportedError;

    private static TimeSpan? calibratedBufferDuration;

    public static void Apply()
    {
        Logger.Info("Starting CriWareEnableExclusiveModePatch");
        criAtom_SetAudioClientShareMode_WASAPI(AudioClientShareMode.Shared); // Load dll

        if (!CheckWaveFormat()) return;
        if (TnTrfMod.Instance.enableCriWarePluginLogging.Value) EnableCriWareLogging();

        engine = new HookEngine();

        criAtomUnity_Initialize_Original = engine.CreateHook("cri_ware_unity.dll", "CRIWARE2EA3E3EA",
            new criAtomUnity_Initialize_Delegate(criAtomUnity_Initialize_Hook));
        if (audioClientInitializeFuncPtr != IntPtr.Zero)
            AudioClientInitializeHook_Original = engine.CreateHook(audioClientInitializeFuncPtr,
                new IAudioClientInitializeHook(AudioClientInitializeHook));

        engine.EnableHooks();
    }

    private static void EnableCriWareLogging()
    {
        var handle = GetModuleHandle("cri_ware_unity.dll");
        var logCallbackPtr = new IntPtr((long)handle + 0x1802273F0L - 0x180000000L);

        WriteMemory(logCallbackPtr, Marshal.GetFunctionPointerForDelegate(new OnCriAtomUnityLog(
            (buffer, _, info, _) =>
            {
                var data1 = info.data1.ToString("X8");
                var data2 = info.data2.ToString("X8");
                var data3 = info.data3.ToString("X8");

                Logger.Info(
                    $"[CriWareUnity] {buffer} ({data1}, {data2}, {data3})");
            }
        )));
    }

    private static bool CheckWaveFormat()
    {
        // Ensure that we can enable this mode.

        var format = GetWaveFormat();

        IAudioClient3? audioClient = null;
        try
        {
            var IID_IAudioClient = typeof(IAudioClient3).GUID;

            // ReSharper disable once SuspiciousTypeConversion.Global
            var realEnumerator = new MMDeviceEnumeratorComObject() as IMMDeviceEnumerator;
            realEnumerator!.GetDefaultAudioEndpoint(0, 0, out var device);

            device.Activate(ref IID_IAudioClient, ClsCtx.ALL, IntPtr.Zero,
                out var audioClient3);
            audioClient = audioClient3 as IAudioClient3;
            if (audioClient == null)
            {
                Logger.Error("Failed to activate IAudioClient3");
                return false;
            }

            var comPtr = Marshal.GetComInterfaceForObject(audioClient, typeof(IAudioClient3));
            var vtable = Marshal.ReadIntPtr(comPtr);
            {
                var start = Marshal.GetStartComSlot(typeof(IAudioClient3));
                // int end = Marshal.GetEndComSlot(typeof(IAudioClient3));
                audioClientInitializeFuncPtr = Marshal.ReadIntPtr(vtable, start * Marshal.SizeOf<IntPtr>());
            }

            audioClient.GetMixFormat(out var mixFormatPtr);
            mixFormat = WaveFormat.MarshalFromPtr(mixFormatPtr);
            Logger.Info("Exclusive mode mix format:");
            PrintWaveFormatInfo(ref mixFormat);

            audioClient.GetDevicePeriod(out _, out var period);
            bufferDuration = new TimeSpan(period);
        }
        catch (COMException e)
        {
            // 0x88890001
            Logger.Error("Failed to initialize exclusive audio client for testing:");
            Logger.Error(e);
            Logger.Error(
                "The wave format of the exclusive audio is invalid and can't be used to initialize exclusive audio, exclusive audio feature is disabled!");
            Logger.Error("\tConfigured wave format:");
            PrintWaveFormatError(ref format);

            return false;
        }
        finally
        {
            if (audioClient != null) Marshal.ReleaseComObject(audioClient);
        }

        Logger.Info($"Exclusive mode buffer duration: {bufferDuration.TotalMilliseconds}ms");

        return true;
    }

    private static void PrintWaveFormatInfo(ref WaveFormat format)
    {
        Logger.Info("\t\t- Wave Format:       " + format.waveFormatTag);
        Logger.Info("\t\t- Channels:          " + format.channels);
        Logger.Info("\t\t- Sample Rate:       " + format.sampleRate);
        Logger.Info("\t\t- Avg Bytes Per Sec: " + format.averageBytesPerSecond);
        Logger.Info("\t\t- Block Align:       " + format.blockAlign);
        Logger.Info("\t\t- Bits Per Sample:   " + format.bitsPerSample);
        Logger.Info("\t\t- CbSize:            " + format.extraSize);
    }

    private static void PrintWaveFormatWarn(ref WaveFormat format)
    {
        Logger.Warn("\t\t- Wave Format:       " + format.waveFormatTag);
        Logger.Warn("\t\t- Channels:          " + format.channels);
        Logger.Warn("\t\t- Sample Rate:       " + format.sampleRate);
        Logger.Warn("\t\t- Avg Bytes Per Sec: " + format.averageBytesPerSecond);
        Logger.Warn("\t\t- Block Align:       " + format.blockAlign);
        Logger.Warn("\t\t- Bits Per Sample:   " + format.bitsPerSample);
        Logger.Warn("\t\t- CbSize:            " + format.extraSize);
    }

    private static void PrintWaveFormatError(ref WaveFormat format)
    {
        Logger.Error("\t\t- Wave Format:       " + format.waveFormatTag);
        Logger.Error("\t\t- Channels:          " + format.channels);
        Logger.Error("\t\t- Sample Rate:       " + format.sampleRate);
        Logger.Error("\t\t- Avg Bytes Per Sec: " + format.averageBytesPerSecond);
        Logger.Error("\t\t- Block Align:       " + format.blockAlign);
        Logger.Error("\t\t- Bits Per Sample:   " + format.bitsPerSample);
        Logger.Error("\t\t- CbSize:            " + format.extraSize);
    }

    private static uint AudioClientInitializeHook(IAudioClient3 audioClient, AudioClientShareMode shareMode,
        AudioClientStreamFlags streamFlags, TimeSpan hnsBufferDuration, TimeSpan hnsPeriodicity, WaveFormat pFormat,
        ref Guid audioSessionGuid)
    {
        uint result;
        if (isFormatSupported)
        {
            var duration = calibratedBufferDuration ?? bufferDuration;
            result = AudioClientInitializeHook_Original!(audioClient, shareMode,
                streamFlags, duration, duration, pFormat,
                ref audioSessionGuid);
        }
        else
        {
            result = AudioClientInitializeHook_Original!(audioClient, AudioClientShareMode.Shared,
                streamFlags, hnsBufferDuration, hnsPeriodicity, pFormat,
                ref audioSessionGuid);
            return result;
        }

        if (result == 0) return result;

        if (result == 0x88890019 && !calibratedBufferDuration.HasValue)
        {
            Logger.Warn("Inappropriate buffer size, recalculating buffer size...");

            audioClient.GetBufferSize(out var frameSize);
            var newBufferSize = 10000.0 * 1000 / pFormat.sampleRate * frameSize + 0.5;
            calibratedBufferDuration = TimeSpan.FromTicks((long)newBufferSize);

            Logger.Warn($"New buffer duration: {calibratedBufferDuration}");

            return result;
        }

        if (!showedUnsupportedError)
        {
            showedUnsupportedError = true;
            Logger.Error(
                $"The wave format of the exclusive audio is invalid and can't be used to initialize exclusive audio (HRESULT: {result:x8}), audio will be disabled!");
            Logger.Error("\tConfigured wave format:");
            PrintWaveFormatError(ref pFormat);
            switch (result)
            {
                case 0x88890019:
                    Logger.Warn("Error meaning: AUDCLNT_E_BUFFER_SIZE_NOT_ALIGNED (The audio buffer is not aligned)");
                    break;
                case 0x8889000a:
                    Logger.Warn(
                        "Error meaning: AUDCLNT_E_DEVICE_IN_USE (The audio device is already in use for other software)");
                    break;
            }

            criAtom_SetAudioClientShareMode_WASAPI(AudioClientShareMode.Shared);
            criAtom_SetAudioClientBufferDuration_WASAPI(TimeSpan.Zero);
            criAtom_SetAudioClientFormat_WASAPI(IntPtr.Zero);
        }

        return AudioClientInitializeHook_Original!(audioClient, AudioClientShareMode.Shared,
            streamFlags, hnsBufferDuration, hnsPeriodicity, pFormat,
            ref audioSessionGuid);
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
            channels = 2,
            sampleRate = TnTrfMod.Instance.exclusiveModeAudioSampleRate.Value,
            bitsPerSample = TnTrfMod.Instance.exclusiveModeAudioBitPerSample.Value,
            extraSize = 0
        };
        if (mixFormat != null)
        {
            format.sampleRate = format.sampleRate == 0 ? mixFormat.sampleRate : format.sampleRate;
            format.bitsPerSample = format.bitsPerSample == 0 ? mixFormat.bitsPerSample : format.bitsPerSample;
        }

        format.blockAlign = (short)(format.bitsPerSample * format.channels / 8);
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
                Logger.Info("The format is supported by the CriWare Unity Plugin, enabling exclusive audio mode");
                criAtom_SetAudioClientShareMode_WASAPI(AudioClientShareMode.Exclusive);
                criAtom_SetAudioClientBufferDuration_WASAPI(bufferDuration);
                criAtom_SetAudioClientFormat_WASAPI(formatPtr);
                Logger.Info("Exclusive audio mode has been setup with format:");
                PrintWaveFormatInfo(ref format);
            }
            else
            {
                Logger.Error(
                    "The wave format of the exclusive audio is not supported by the CriWare Unity Plugin, exclusive audio feature is disabled!");
                Logger.Error("\tConfigured wave format:");
                PrintWaveFormatError(ref format);
            }

            Marshal.FreeHGlobal(formatPtr);
        }

        Logger.Info("Running criAtomUnity_Initialize_Original");
        var result = criAtomUnity_Initialize_Original!();
        Logger.Info($"Result criAtomUnity_Initialize_Original: {result}");
        return result;
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
    private delegate uint IAudioClientInitializeHook(
        [MarshalAs(UnmanagedType.Interface)] IAudioClient3 audioClient,
        AudioClientShareMode shareMode,
        AudioClientStreamFlags streamFlags,
        TimeSpan hnsBufferDuration, // REFERENCE_TIME
        TimeSpan hnsPeriodicity, // REFERENCE_TIME
        [In] WaveFormat pFormat,
        [In] ref Guid audioSessionGuid
    );

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void OnCriAtomUnityLog(string msgBuffer, int level, LoggingData info, IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate long criAtomUnity_Initialize_Delegate();
}