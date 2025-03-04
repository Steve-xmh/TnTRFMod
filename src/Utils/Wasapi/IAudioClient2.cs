using System.Runtime.InteropServices;

namespace TnTRFMod.Utils.Wasapi;

// 得益于 COM 接口继承和 .NET 类型互操作性的差异，我们不得不对 IAudioClient2 编写它的所有继承接口
// 参考链接： https://learn.microsoft.com/en-us/dotnet/standard/native-interop/qualify-net-types-for-interoperation#com-interface-inheritance-and-net

[ComImport]
[Guid("726778CD-F60A-4eda-82DE-E47610CD78AA")]
internal interface IAudioClient2 : IAudioClient
{
    #region IAudioClient

    [PreserveSig]
    new int Initialize(AudioClientShareMode shareMode,
        AudioClientStreamFlags streamFlags,
        long hnsBufferDuration, // REFERENCE_TIME
        long hnsPeriodicity, // REFERENCE_TIME
        [In] WaveFormat pFormat,
        [In] ref Guid audioSessionGuid);

    new int GetBufferSize(out uint bufferSize);

    [return: MarshalAs(UnmanagedType.I8)]
    new long GetStreamLatency();

    new int GetCurrentPadding(out int currentPadding);

    [PreserveSig]
    new int IsFormatSupported(
        AudioClientShareMode shareMode,
        [In] WaveFormat pFormat,
        IntPtr closestMatchFormat); // or outIntPtr??

    new int GetMixFormat(out IntPtr deviceFormatPointer);

    new int GetDevicePeriod(out long defaultDevicePeriod, out long minimumDevicePeriod);

    new int Start();

    new int Stop();

    new int Reset();

    new int SetEventHandle(IntPtr eventHandle);

    [PreserveSig]
    new int GetService([In] [MarshalAs(UnmanagedType.LPStruct)] Guid interfaceId,
        [Out] [MarshalAs(UnmanagedType.IUnknown)]
        out object interfacePointer);

    #endregion

    void IsOffloadCapable(AudioStreamCategory category, out bool pbOffloadCapable);

    void SetClientProperties([In] IntPtr pProperties);

    void GetBufferSizeLimits(IntPtr pFormat, bool bEventDriven,
        out long phnsMinBufferDuration, out long phnsMaxBufferDuration);
}

/// <summary>
///     Specifies the category of an audio stream.
///     https://docs.microsoft.com/en-us/windows/win32/api/audiosessiontypes/ne-audiosessiontypes-audio_stream_category
///     AUDIO_STREAM_CATEGORY
/// </summary>
public enum AudioStreamCategory
{
    /// <summary>Other audio stream.</summary>
    Other,

    /// <summary>
    ///     Media that will only stream when the app is in the foreground.
    /// </summary>
    ForegroundOnlyMedia,

    /// <summary>
    ///     Media that can be streamed when the app is in the background.
    /// </summary>
    BackgroundCapableMedia,

    /// <summary>Real-time communications, such as VOIP or chat.</summary>
    Communications,

    /// <summary>Alert sounds.</summary>
    Alerts,

    /// <summary>Sound effects.</summary>
    SoundEffects,

    /// <summary>Game sound effects.</summary>
    GameEffects,

    /// <summary>Background audio for games.</summary>
    GameMedia,

    /// <summary>
    ///     Game chat audio. Similar to AudioCategory_Communications except that AudioCategory_GameChat will not attenuate
    ///     other streams.
    /// </summary>
    GameChat,

    /// <summary>Speech</summary>
    Speech,

    /// <summary>Stream that includes audio with dialog.</summary>
    Movie,

    /// <summary>Stream that includes audio without dialog.</summary>
    Media,

    /// <summary>
    ///     Media is audio captured with the intent of capturing voice sources located in the ‘far field’. (Far away from the
    ///     microphone.)
    /// </summary>
    FarFieldSpeech,

    /// <summary>
    ///     Media is captured audio that requires consistent speech processing for the captured audio stream across all Windows
    ///     devices. Used by applications that process speech data using machine learning algorithms.
    /// </summary>
    UniformSpeech,

    /// <summary>
    ///     Media is audio captured with the intent of enabling dictation or typing by voice.
    /// </summary>
    VoiceTyping
}