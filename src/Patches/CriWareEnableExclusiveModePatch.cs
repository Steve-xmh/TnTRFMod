using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using MinHook;
using TnTRFMod.Utils;
using TnTRFMod.Utils.Wasapi;
using AudioClientShareMode = TnTRFMod.Utils.Wasapi.AudioClientShareMode;
using AudioClientStreamFlags = TnTRFMod.Utils.Wasapi.AudioClientStreamFlags;

namespace TnTRFMod.Patches;

[SuppressMessage("Interoperability", "CA1416")]
public static class CriWareEnableExclusiveModePatch
{
    private const int CriWareStandardThreadModel = 0;
    private const int CriWareLowLatencyThreadModel = 4;
    private const int CriWareAtomInitializeOffset = 0x2C528;
    private const int CriWareThreadModelOffset = 0x1E5C78;
    private const uint AUDCLNT_E_DEVICE_IN_USE = 0x8889000A;
    private const uint AUDCLNT_E_BUFFER_SIZE_NOT_ALIGNED = 0x88890019;
    private const uint AUDCLNT_E_UNSUPPORTED_FORMAT = 0x88890008;
    private const uint E_INVALIDARG = 0x80070057;
    private static readonly TimeSpan ProAudioSchedulingThreshold = TimeSpan.FromMilliseconds(10);
    private static readonly TimeSpan PreferredManualEventFallbackDuration = TimeSpan.FromMilliseconds(9);

    private static readonly Guid KSDATAFORMAT_SUBTYPE_PCM = new("00000001-0000-0010-8000-00aa00389b71");
    private static readonly Guid KSDATAFORMAT_SUBTYPE_IEEE_FLOAT = new("00000003-0000-0010-8000-00aa00389b71");

    private static HookEngine? engine;
    private static IntPtr criWareAtomInitializePtr = IntPtr.Zero;
    private static IntPtr criWareThreadModelPtr = IntPtr.Zero;

    private static WaveFormat? mixFormat;
    private static WaveFormat? exclusiveFormat;
    private static TimeSpan bufferDuration = TimeSpan.Zero;
    private static TimeSpan minimumBufferDuration = TimeSpan.Zero;
    private static TimeSpan defaultBufferDuration = TimeSpan.Zero;

    private static CriWarePluginNative.IAudioClientInitializeHook? AudioClientInitializeHook_Original;
    private static CriWarePluginNative.CriAtomInitializeHook? CriAtomInitializeHook_Original;

    private static
        IntPtr audioClientInitializeFuncPtr = IntPtr.Zero;

    private static bool showedUnsupportedError;
    private static bool loggedAutomaticExclusiveBufferDuration;
    private static bool loggedManualEventFallbackDuration;
    private static int? originalCriWareThreadModel;
    private static bool skipInitializeHook;
    private static bool loggedCriWareInitThreadModelOverride;

    private static TimeSpan? calibratedBufferDuration;

    private static bool applied;

    public static void Apply()
    {
        if (applied)
        {
            Logger.Warn("CriWareEnableExclusiveModePatch is already active, skipping duplicated apply.");
            return;
        }

        Logger.Info("Starting CriWareEnableExclusiveModePatch");
        CriWarePluginNative.CriAtomWASAPI.SetAudioClientShareMode(AudioClientShareMode.Shared); // Load dll
        ApplyCriWareCompatibleThreadModel();
        showedUnsupportedError = false;
        calibratedBufferDuration = null;

        if (!CheckWaveFormat()) return;
        if (true) EnableCriWareLogging();

        engine = new HookEngine();
        InstallCriWareThreadModelHook();

        if (!skipInitializeHook && audioClientInitializeFuncPtr == IntPtr.Zero)
        {
            Logger.Error(
                "Failed to get IAudioClient3::Initialize function pointer, exclusive audio feature is disabled!");
            return;
        }

        var targetFormat = GetWaveFormat();
        Logger.Info("Configured exclusive audio format:");
        PrintWaveFormatInfo(targetFormat);
        Logger.Info($"Buffer duration: {bufferDuration.TotalMilliseconds}ms");

        ApplyCriWareWasapiSettings(targetFormat, bufferDuration);

        if (skipInitializeHook)
            Logger.Info(
                "Skipping global IAudioClient::Initialize hook because CriWare is using standard-mode WASAPI initialization.");
        else
            AudioClientInitializeHook_Original = engine.CreateHook(audioClientInitializeFuncPtr,
                new CriWarePluginNative.IAudioClientInitializeHook(AudioClientInitializeHook));

        engine.EnableHooks();

        applied = true;
        Logger.Message("Exclusive audio client feature is ready, waiting for game's initialization");
        Logger.Info(
            "If the game has showed title screen but still not audio, maybe the game is failed to enable exclusive audio.");
        Logger.Info("You can try enable CriWare Plugin logging in config file for further logs.");
    }

    public static void Reset()
    {
        ResetCriWareWasapiSettings();
        RestoreCriWareThreadModel();
        if (engine is IDisposable disposable)
            disposable.Dispose();

        engine = null;
        AudioClientInitializeHook_Original = null;
        CriAtomInitializeHook_Original = null;
        criWareAtomInitializePtr = IntPtr.Zero;
        audioClientInitializeFuncPtr = IntPtr.Zero;
        calibratedBufferDuration = null;
        showedUnsupportedError = false;
        loggedAutomaticExclusiveBufferDuration = false;
        loggedManualEventFallbackDuration = false;
        loggedCriWareInitThreadModelOverride = false;
        exclusiveFormat = null;
        minimumBufferDuration = TimeSpan.Zero;
        defaultBufferDuration = TimeSpan.Zero;
        skipInitializeHook = false;
        applied = false;
    }

    private static void ApplyCriWareCompatibleThreadModel()
    {
        var handle = GetModuleHandle("cri_ware_unity.dll");
        if (handle == IntPtr.Zero)
        {
            Logger.Warn("Failed to get cri_ware_unity.dll handle, keeping CriWare thread model unchanged.");
            return;
        }

        criWareThreadModelPtr = IntPtr.Add(handle, CriWareThreadModelOffset);
        criWareAtomInitializePtr = IntPtr.Add(handle, CriWareAtomInitializeOffset);
        var currentThreadModel = Marshal.ReadInt32(criWareThreadModelPtr);
        originalCriWareThreadModel ??= currentThreadModel;

        if (currentThreadModel != CriWareLowLatencyThreadModel)
        {
            skipInitializeHook = true;
            Logger.Info(
                $"CriWare thread model is {currentThreadModel}, keeping existing non-low-latency mode for exclusive WASAPI.");
            return;
        }

        WriteMemory(criWareThreadModelPtr, CriWareStandardThreadModel);
        skipInitializeHook = true;
        Logger.Warn(
            "Switched CriWare thread model from low-latency mode 4 to standard mode 0 for exclusive WASAPI compatibility.");
    }

    private static void InstallCriWareThreadModelHook()
    {
        if (criWareAtomInitializePtr == IntPtr.Zero)
        {
            Logger.Warn(
                "Failed to resolve CriWare Atom initialize function, thread model may switch back to low-latency mode later.");
            return;
        }

        CriAtomInitializeHook_Original = engine!.CreateHook(criWareAtomInitializePtr,
            new CriWarePluginNative.CriAtomInitializeHook(CriAtomInitializeHook));
    }

    private static long CriAtomInitializeHook(IntPtr config, int useAtomExAsr, IntPtr work, int workSize)
    {
        var result = CriAtomInitializeHook_Original!(config, useAtomExAsr, work, workSize);
        ForceCriWareStandardThreadModel("after CriAtom initialization");
        return result;
    }

    private static void RestoreCriWareThreadModel()
    {
        if (criWareThreadModelPtr == IntPtr.Zero || !originalCriWareThreadModel.HasValue)
            return;

        WriteMemory(criWareThreadModelPtr, originalCriWareThreadModel.Value);
        Logger.Info($"Restored CriWare thread model to {originalCriWareThreadModel.Value}.");
        criWareThreadModelPtr = IntPtr.Zero;
        originalCriWareThreadModel = null;
    }

    private static void ForceCriWareStandardThreadModel(string reason)
    {
        if (criWareThreadModelPtr == IntPtr.Zero)
            return;

        var currentThreadModel = Marshal.ReadInt32(criWareThreadModelPtr);
        if (currentThreadModel == CriWareStandardThreadModel)
            return;

        WriteMemory(criWareThreadModelPtr, CriWareStandardThreadModel);
        if (!loggedCriWareInitThreadModelOverride)
        {
            Logger.Warn($"Forced CriWare thread model from {currentThreadModel} back to 0 {reason}.");
            loggedCriWareInitThreadModelOverride = true;
        }
    }

    private static void EnableCriWareLogging()
    {
        var handle = GetModuleHandle("cri_ware_unity.dll");
        var logCallbackPtr = new IntPtr((long)handle + 0x1802273F0L - 0x180000000L);

        WriteMemory(logCallbackPtr, Marshal.GetFunctionPointerForDelegate(
            new CriWarePluginNative.OnCriAtomUnityLog((buffer, _, info, _) =>
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
        IMMDevice? device = null;
        IMMDeviceEnumerator? enumerator = null;
        var mixFormatPtr = IntPtr.Zero;
        try
        {
            var IID_IAudioClient = typeof(IAudioClient3).GUID;

            // ReSharper disable once SuspiciousTypeConversion.Global
            enumerator = new MMDeviceEnumeratorComObject() as IMMDeviceEnumerator;
            enumerator!.GetDefaultAudioEndpoint(0, 0, out device);

            if (device == null)
            {
                Logger.Error("Failed to get default audio endpoint, exclusive audio feature is disabled!");
                return false;
            }

            device.Activate(ref IID_IAudioClient, ClsCtx.ALL, IntPtr.Zero,
                out var audioClient3);
            audioClient = audioClient3 as IAudioClient3;
            if (audioClient == null)
            {
                Logger.Error("Failed to activate IAudioClient3, exclusive audio feature is disabled!");
                return false;
            }

            var comPtr = Marshal.GetComInterfaceForObject(audioClient, typeof(IAudioClient3));
            var vtable = Marshal.ReadIntPtr(comPtr);
            {
                var start = Marshal.GetStartComSlot(typeof(IAudioClient3));
                // int end = Marshal.GetEndComSlot(typeof(IAudioClient3));
                audioClientInitializeFuncPtr = Marshal.ReadIntPtr(vtable, start * Marshal.SizeOf<IntPtr>());
            }

            audioClient.GetMixFormat(out mixFormatPtr);
            mixFormat = WaveFormat.MarshalFromPtr(mixFormatPtr);
            Logger.Info("Shared mode mix format:");
            PrintWaveFormatInfo(mixFormat);

            audioClient.GetDevicePeriod(out var defaultPeriod, out var minimumPeriod);
            defaultBufferDuration = new TimeSpan(defaultPeriod);
            minimumBufferDuration = new TimeSpan(minimumPeriod);

            if (!TrySelectExclusiveFormat(audioClient, out exclusiveFormat))
            {
                Logger.Error(
                    "Failed to find any driver-supported exclusive audio format, exclusive audio feature is disabled!");
                return false;
            }

            Logger.Info("Selected driver-supported exclusive format:");
            PrintWaveFormatInfo(exclusiveFormat);

            var useEventDrivenBufferModel = !skipInitializeHook;
            bufferDuration = DetermineExclusiveBufferDuration(audioClient, exclusiveFormat, defaultPeriod,
                minimumPeriod,
                useEventDrivenBufferModel);
        }
        catch (COMException e)
        {
            // 0x88890001
            Logger.Error("Failed to initialize exclusive audio client for testing:");
            Logger.Error(e);
            Logger.Error(
                "The wave format of the exclusive audio is invalid and can't be used to initialize exclusive audio, exclusive audio feature is disabled!");
            Logger.Error("\tConfigured wave format:");
            PrintWaveFormatError(format);

            return false;
        }
        finally
        {
            if (mixFormatPtr != IntPtr.Zero) Marshal.FreeCoTaskMem(mixFormatPtr);
            if (device != null) Marshal.ReleaseComObject(device);
            if (enumerator != null) Marshal.ReleaseComObject(enumerator);
            if (audioClient != null) Marshal.ReleaseComObject(audioClient);
        }

        Logger.Info($"Exclusive mode minimum buffer duration: {minimumBufferDuration.TotalMilliseconds}ms");
        Logger.Info($"Exclusive mode default buffer duration: {defaultBufferDuration.TotalMilliseconds}ms");
        Logger.Info($"Exclusive mode selected buffer duration: {bufferDuration.TotalMilliseconds}ms");

        return true;
    }

    private static void PrintWaveFormatInfo(WaveFormat format)
    {
        Logger.Info("\t\t- Wave Format:       " + format.waveFormatTag);
        Logger.Info("\t\t- Channels:          " + format.channels);
        Logger.Info("\t\t- Sample Rate:       " + format.sampleRate);
        Logger.Info("\t\t- Avg Bytes Per Sec: " + format.averageBytesPerSecond);
        Logger.Info("\t\t- Block Align:       " + format.blockAlign);
        Logger.Info("\t\t- Bits Per Sample:   " + format.bitsPerSample);
        Logger.Info("\t\t- CbSize:            " + format.extraSize);
        if (format is WaveFormatExtensible extensible)
        {
            Logger.Info("\t\t- Valid Bits:        " + extensible.wValidBitsPerSample);
            Logger.Info($"\t\t- Channel Mask:      0x{extensible.dwChannelMask:X}");
            Logger.Info("\t\t- Sub Format:        " + extensible.subFormat);
        }
    }

    private static void PrintWaveFormatError(WaveFormat format)
    {
        Logger.Error("\t\t- Wave Format:       " + format.waveFormatTag);
        Logger.Error("\t\t- Channels:          " + format.channels);
        Logger.Error("\t\t- Sample Rate:       " + format.sampleRate);
        Logger.Error("\t\t- Avg Bytes Per Sec: " + format.averageBytesPerSecond);
        Logger.Error("\t\t- Block Align:       " + format.blockAlign);
        Logger.Error("\t\t- Bits Per Sample:   " + format.bitsPerSample);
        Logger.Error("\t\t- CbSize:            " + format.extraSize);
        if (format is WaveFormatExtensible extensible)
        {
            Logger.Error("\t\t- Valid Bits:        " + extensible.wValidBitsPerSample);
            Logger.Error($"\t\t- Channel Mask:      0x{extensible.dwChannelMask:X}");
            Logger.Error("\t\t- Sub Format:        " + extensible.subFormat);
        }
    }

    private static uint AudioClientInitializeHook(IAudioClient3 audioClient, AudioClientShareMode shareMode,
        AudioClientStreamFlags streamFlags, TimeSpan hnsBufferDuration, TimeSpan hnsPeriodicity, IntPtr pFormat,
        ref Guid audioSessionGuid)
    {
        var format = WaveFormat.MarshalFromPtr(pFormat);
        if (shareMode != AudioClientShareMode.Exclusive)
            return AudioClientInitializeHook_Original!(audioClient, shareMode, streamFlags, hnsBufferDuration,
                hnsPeriodicity, pFormat, ref audioSessionGuid);

        var isEventDriven = (streamFlags & AudioClientStreamFlags.EventCallback) != 0;
        if (isEventDriven)
        {
            var automaticResult = AudioClientInitializeHook_Original!(audioClient, shareMode,
                streamFlags, hnsBufferDuration, hnsPeriodicity, pFormat,
                ref audioSessionGuid);
            if (automaticResult == 0)
            {
                LogActualExclusiveBufferDuration(audioClient, format, "automatic");
                Logger.Message("Exclusive audio client initialized successfully with automatic event buffer sizing!");
                return automaticResult;
            }

            Logger.Warn(
                $"Exclusive automatic event buffer initialization failed with 0x{automaticResult:X8}, retrying with manual buffer duration.");

            if (automaticResult != E_INVALIDARG && automaticResult != AUDCLNT_E_BUFFER_SIZE_NOT_ALIGNED)
                Logger.Warn(
                    "Driver rejected CriWare's native automatic event-buffer sizing; falling back to fixed exclusive buffer duration.");
        }

        var duration = calibratedBufferDuration ?? bufferDuration;
        if (isEventDriven)
            duration = DetermineManualEventFallbackDuration(duration);

        if (duration != bufferDuration && !calibratedBufferDuration.HasValue)
        {
            bufferDuration = duration;
            CriWarePluginNative.CriAtomWASAPI.SetAudioClientBufferDuration(bufferDuration);
        }

        var result = AudioClientInitializeHook_Original!(audioClient, shareMode,
            streamFlags, duration, duration, pFormat,
            ref audioSessionGuid);

        switch (result)
        {
            case 0:
                LogActualExclusiveBufferDuration(audioClient, format, "manual");
                Logger.Message("Exclusive audio client initialized successfully!");
                return result;
            case AUDCLNT_E_BUFFER_SIZE_NOT_ALIGNED when !calibratedBufferDuration.HasValue:
            {
                Logger.Warn("Inappropriate buffer size, recalculating buffer size...");

                audioClient.GetBufferSize(out var frameSize);
                calibratedBufferDuration = TimeSpan.FromTicks((long)Math.Ceiling(
                    TimeSpan.TicksPerSecond * frameSize / (double)format.sampleRate));
                bufferDuration = calibratedBufferDuration.Value;
                CriWarePluginNative.CriAtomWASAPI.SetAudioClientBufferDuration(bufferDuration);

                Logger.Warn(
                    $"Retrying exclusive initialization with aligned buffer duration {bufferDuration.TotalMilliseconds:F3}ms");

                result = AudioClientInitializeHook_Original(audioClient, shareMode,
                    streamFlags, bufferDuration, bufferDuration, pFormat,
                    ref audioSessionGuid);
                if (result == 0)
                {
                    LogActualExclusiveBufferDuration(audioClient, format, "aligned-manual");
                    Logger.Message("Exclusive audio client initialized successfully after buffer alignment retry!");
                    return result;
                }

                break;
            }
        }

        if (showedUnsupportedError)
            return AudioClientInitializeHook_Original(audioClient, AudioClientShareMode.Shared,
                streamFlags, TimeSpan.Zero, TimeSpan.Zero, pFormat,
                ref audioSessionGuid);

        showedUnsupportedError = true;
        Logger.Error(
            $"The wave format of the exclusive audio is invalid and can't be used to initialize exclusive audio (HRESULT: {result:x8}), audio will be disabled!");
        Logger.Error("\tConfigured wave format:");
        PrintWaveFormatError(format);
        switch (result)
        {
            case AUDCLNT_E_BUFFER_SIZE_NOT_ALIGNED:
                Logger.Warn("Error meaning: AUDCLNT_E_BUFFER_SIZE_NOT_ALIGNED (The audio buffer is not aligned)");
                break;
            case AUDCLNT_E_DEVICE_IN_USE:
                Logger.Warn(
                    "Error meaning: AUDCLNT_E_DEVICE_IN_USE (The audio device is already in use for other software)");
                break;
            case AUDCLNT_E_UNSUPPORTED_FORMAT:
                Logger.Warn(
                    "Error meaning: AUDCLNT_E_UNSUPPORTED_FORMAT (The device driver rejected this exclusive format)");
                break;
        }

        ResetCriWareWasapiSettings();

        return AudioClientInitializeHook_Original(audioClient, AudioClientShareMode.Shared,
            streamFlags, TimeSpan.Zero, TimeSpan.Zero, pFormat,
            ref audioSessionGuid);
    }

    private static void ApplyCriWareWasapiSettings(WaveFormat targetFormat, TimeSpan targetBufferDuration)
    {
        CriWarePluginNative.CriAtomWASAPI.SetAudioClientShareMode(AudioClientShareMode.Exclusive);
        CriWarePluginNative.CriAtomWASAPI.SetAudioClientBufferDuration(targetBufferDuration);

        var formatPtr = targetFormat.MarshalToPtr();
        try
        {
            CriWarePluginNative.CriAtomWASAPI.SetAudioClientFormat(formatPtr);
        }
        finally
        {
            Marshal.FreeHGlobal(formatPtr);
        }
    }

    private static void ResetCriWareWasapiSettings()
    {
        CriWarePluginNative.CriAtomWASAPI.SetAudioClientShareMode(AudioClientShareMode.Shared);
        CriWarePluginNative.CriAtomWASAPI.SetAudioClientBufferDuration(TimeSpan.Zero);
        CriWarePluginNative.CriAtomWASAPI.SetAudioClientFormat(IntPtr.Zero);
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
        var format = exclusiveFormat?.Clone() ?? mixFormat?.Clone() ?? new WaveFormat
        {
            waveFormatTag = WaveFormatEncoding.Pcm,
            channels = 2,
            sampleRate = 48000,
            bitsPerSample = 16,
            extraSize = 0
        };

        format.channels = 2;
        // format.sampleRate = TnTrfMod.Instance.exclusiveModeAudioSampleRate.Value;
        // format.bitsPerSample = TnTrfMod.Instance.exclusiveModeAudioBitPerSample.Value;

        if (format is WaveFormatExtensible { wValidBitsPerSample: 0 } extensible)
            extensible.wValidBitsPerSample = extensible.bitsPerSample;

        format.blockAlign = (short)(format.bitsPerSample * format.channels / 8);
        format.averageBytesPerSecond = format.sampleRate * format.blockAlign;
        return format;
    }

    private static bool TrySelectExclusiveFormat(IAudioClient3 audioClient, out WaveFormat format)
    {
        Logger.Info("Probing driver-supported exclusive formats...");

        foreach (var candidate in BuildExclusiveFormatCandidates())
        {
            var result = audioClient.IsFormatSupported(AudioClientShareMode.Exclusive, candidate, IntPtr.Zero);
            if (result >= 0)
            {
                Logger.Info($"Exclusive format probe succeeded with 0x{result:X8}:");
                format = candidate;
                return true;
            }

            Logger.Info($"Exclusive format probe failed with 0x{unchecked((uint)result):X8}:");
            PrintWaveFormatInfo(candidate);
        }

        format = null!;
        return false;
    }

    private static IEnumerable<WaveFormat> BuildExclusiveFormatCandidates()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var candidate in EnumerateExclusiveFormatCandidates())
        {
            var key =
                $"{candidate.waveFormatTag}|{candidate.channels}|{candidate.sampleRate}|{candidate.bitsPerSample}|{candidate.extraSize}";
            if (seen.Add(key))
                yield return candidate;
        }
    }

    private static IEnumerable<WaveFormat> EnumerateExclusiveFormatCandidates()
    {
        var sampleRate = mixFormat?.sampleRate ?? 48000;
        var channels = (short)2;

        var simpleMixFormat = TryCreateSimpleExclusiveFormat(mixFormat);
        if (simpleMixFormat != null)
            yield return simpleMixFormat;

        yield return Create24BitIn32ContainerWaveFormat(sampleRate, channels);
        yield return CreateSimpleWaveFormat(WaveFormatEncoding.IeeeFloat, sampleRate, 32, channels);
        yield return CreateSimpleWaveFormat(WaveFormatEncoding.Pcm, sampleRate, 16, channels);

        if (sampleRate != 44100)
        {
            yield return Create24BitIn32ContainerWaveFormat(44100, channels);
            yield return CreateSimpleWaveFormat(WaveFormatEncoding.IeeeFloat, 44100, 32, channels);
            yield return CreateSimpleWaveFormat(WaveFormatEncoding.Pcm, 44100, 16, channels);
        }
    }

    private static WaveFormat? TryCreateSimpleExclusiveFormat(WaveFormat? source)
    {
        if (source == null)
            return null;

        if (source.waveFormatTag == WaveFormatEncoding.Pcm || source.waveFormatTag == WaveFormatEncoding.IeeeFloat)
            return CreateSimpleWaveFormat(source.waveFormatTag, source.sampleRate, source.bitsPerSample, 2);

        if (source is not WaveFormatExtensible extensible) return null;

        if (extensible.subFormat == KSDATAFORMAT_SUBTYPE_PCM &&
            extensible is { bitsPerSample: 32, wValidBitsPerSample: 24 })
            return Create24BitIn32ContainerWaveFormat(extensible.sampleRate, 2);

        if (extensible.subFormat == KSDATAFORMAT_SUBTYPE_PCM)
            return CreateSimpleWaveFormat(WaveFormatEncoding.Pcm, extensible.sampleRate, extensible.bitsPerSample,
                2);

        if (extensible.subFormat == KSDATAFORMAT_SUBTYPE_IEEE_FLOAT)
            return CreateSimpleWaveFormat(WaveFormatEncoding.IeeeFloat, extensible.sampleRate,
                extensible.bitsPerSample, 2);

        return null;
    }

    // mixFormat
    private static WaveFormat CreateSimpleWaveFormat(WaveFormatEncoding encoding, int sampleRate, short bitsPerSample,
        short channels)
    {
        var format = new WaveFormat
        {
            waveFormatTag = encoding,
            channels = channels,
            sampleRate = sampleRate,
            bitsPerSample = bitsPerSample,
            extraSize = 0
        };
        format.blockAlign = (short)(format.bitsPerSample * format.channels / 8);
        format.averageBytesPerSecond = format.sampleRate * format.blockAlign;
        return format;
    }

    private static WaveFormat Create24BitIn32ContainerWaveFormat(int sampleRate, short channels)
    {
        var format = new WaveFormatExtensible
        {
            waveFormatTag = WaveFormatEncoding.Extensible,
            channels = channels,
            sampleRate = sampleRate,
            bitsPerSample = 32,
            extraSize = 22,
            wValidBitsPerSample = 24,
            dwChannelMask = channels switch
            {
                1 => 0x4,
                2 => 0x3,
                _ => 0
            },
            subFormat = KSDATAFORMAT_SUBTYPE_PCM
        };
        format.blockAlign = (short)(format.bitsPerSample * format.channels / 8);
        format.averageBytesPerSecond = format.sampleRate * format.blockAlign;
        return format;
    }

    private static TimeSpan DetermineExclusiveBufferDuration(IAudioClient3 audioClient, WaveFormat format,
        long defaultPeriod, long minimumPeriod, bool eventDriven)
    {
        var selectedTicks = SelectInitialExclusiveBufferTicks(defaultPeriod, minimumPeriod, eventDriven);
        var formatPtr = format.MarshalToPtr();
        try
        {
            audioClient.GetBufferSizeLimits(formatPtr, eventDriven, out var minimumLimit, out var maximumLimit);
            if (minimumLimit > 0 && selectedTicks < minimumLimit)
                selectedTicks = minimumLimit;
            if (maximumLimit > 0 && selectedTicks > maximumLimit)
                selectedTicks = maximumLimit;

            var modeName = eventDriven ? "event" : "polling";
            Logger.Info(
                $"Exclusive mode {modeName} buffer limits: min={TimeSpan.FromTicks(minimumLimit).TotalMilliseconds}ms max={TimeSpan.FromTicks(maximumLimit).TotalMilliseconds}ms");
        }
        catch (COMException e)
        {
            Logger.Warn(
                $"Failed to query exclusive {(eventDriven ? "event" : "polling")} buffer limits, using device default period instead. HRESULT=0x{e.HResult:X8}");
        }
        finally
        {
            Marshal.FreeHGlobal(formatPtr);
        }

        return TimeSpan.FromTicks(selectedTicks);
    }

    private static long SelectInitialExclusiveBufferTicks(long defaultPeriod, long minimumPeriod, bool eventDriven)
    {
        if (eventDriven)
            return defaultPeriod > 0 ? defaultPeriod : minimumPeriod;

        var selectedTicks = minimumPeriod > 0 ? minimumPeriod : defaultPeriod;
        if (selectedTicks <= 0)
            selectedTicks = defaultPeriod > 0 ? defaultPeriod : minimumPeriod;

        var selectedDuration = TimeSpan.FromTicks(selectedTicks);
        Logger.Info(
            $"Using {(eventDriven ? "event" : "polling")} exclusive buffer target {selectedDuration.TotalMilliseconds:F3}ms before driver limit clamping.");
        return selectedTicks;
    }

    private static TimeSpan DetermineManualEventFallbackDuration(TimeSpan currentDuration)
    {
        if (currentDuration < ProAudioSchedulingThreshold)
            return currentDuration;

        if (minimumBufferDuration <= TimeSpan.Zero || minimumBufferDuration >= ProAudioSchedulingThreshold)
            return currentDuration;

        var fallbackDuration = PreferredManualEventFallbackDuration;
        if (fallbackDuration <= minimumBufferDuration)
            fallbackDuration = minimumBufferDuration + TimeSpan.FromTicks(TimeSpan.TicksPerMillisecond);

        if (fallbackDuration >= ProAudioSchedulingThreshold)
            fallbackDuration = TimeSpan.FromTicks(ProAudioSchedulingThreshold.Ticks - 1);

        if (!loggedManualEventFallbackDuration)
        {
            Logger.Warn(
                $"Switching exclusive manual event buffer duration from {currentDuration.TotalMilliseconds:F3}ms to {fallbackDuration.TotalMilliseconds:F3}ms to request Pro Audio scheduling on USB/WaveCyclic devices.");
            loggedManualEventFallbackDuration = true;
        }

        return fallbackDuration;
    }

    private static void LogActualExclusiveBufferDuration(IAudioClient3 audioClient, WaveFormat format, string mode)
    {
        if (loggedAutomaticExclusiveBufferDuration && mode == "automatic")
            return;

        try
        {
            audioClient.GetBufferSize(out var bufferFrames);
            var actualDuration = TimeSpan.FromTicks((long)Math.Ceiling(
                TimeSpan.TicksPerSecond * bufferFrames / (double)format.sampleRate));
            Logger.Info(
                $"Exclusive {mode} buffer resolved to {bufferFrames} frames ({actualDuration.TotalMilliseconds:F3}ms).");
            if (mode == "automatic")
                loggedAutomaticExclusiveBufferDuration = true;
        }
        catch (COMException e)
        {
            Logger.Warn($"Failed to query actual exclusive buffer size after initialization. HRESULT=0x{e.HResult:X8}");
        }
    }


    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32", PreserveSig = true)]
    private static extern bool VirtualProtect(IntPtr lpAddress, int dwSize, uint flNewProtect, out uint lpflOldProtect);

    private static class CriWarePluginNative
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate long CriAtomInitializeHook(IntPtr config, int useAtomExAsr, IntPtr work, int workSize);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate uint IAudioClientInitializeHook(
            [MarshalAs(UnmanagedType.Interface)] IAudioClient3 audioClient,
            AudioClientShareMode shareMode,
            AudioClientStreamFlags streamFlags,
            TimeSpan hnsBufferDuration, // REFERENCE_TIME
            TimeSpan hnsPeriodicity, // REFERENCE_TIME
            IntPtr pFormat,
            [In] ref Guid audioSessionGuid
        );

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void OnCriAtomUnityLog(string msgBuffer, int level, LoggingData info, IntPtr data);

        public static class CriAtomWASAPI
        {
            private const string CriWarePluginName =
                "Taiko no Tatsujin Rhythm Festival_Data/Plugins/x86_64/cri_ware_unity.dll";

            // criAtom_SetAudioClientShareMode_WASAPI
            [DllImport(CriWarePluginName, EntryPoint = "criAtom_SetAudioClientShareMode_WASAPI",
                CallingConvention = CallingConvention.StdCall)]
            public static extern void SetAudioClientShareMode(AudioClientShareMode mode);

            [DllImport(CriWarePluginName, EntryPoint = "criAtom_SetAudioClientFormat_WASAPI",
                CallingConvention = CallingConvention.StdCall)]
            public static extern void SetAudioClientFormat(IntPtr mode);

            [DllImport(CriWarePluginName, EntryPoint = "criAtom_SetAudioClientBufferDuration_WASAPI",
                CallingConvention = CallingConvention.StdCall)]
            public static extern void SetAudioClientBufferDuration(TimeSpan duration);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 2)]
        public struct LoggingData
        {
            public long data1;
            public long data2;
            public long data3;
        }
    }
}