using System.Runtime.InteropServices;

// ReSharper disable UnusedMember.Global

namespace TnTRFMod.Utils.Wasapi;

// 得益于 COM 接口继承和 .NET 类型互操作性的差异，我们不得不对 IAudioClient3 编写它的所有继承接口
// 参考链接： https://learn.microsoft.com/en-us/dotnet/standard/native-interop/qualify-net-types-for-interoperation#com-interface-inheritance-and-net

[ComImport]
[Guid("7ED4EE07-8E67-4CD4-8C1A-2B7A5987AD42")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioClient3
{
    #region IAudioClient

    [PreserveSig]
    int Initialize(AudioClientShareMode shareMode,
        AudioClientStreamFlags streamFlags,
        TimeSpan hnsBufferDuration, // REFERENCE_TIME
        TimeSpan hnsPeriodicity, // REFERENCE_TIME
        [In] WaveFormat pFormat,
        [In] ref Guid audioSessionGuid);

    int GetBufferSize(out uint bufferSize);

    [return: MarshalAs(UnmanagedType.I8)]
    long GetStreamLatency();

    int GetCurrentPadding(out int currentPadding);

    [PreserveSig]
    int IsFormatSupported(
        AudioClientShareMode shareMode,
        [In] WaveFormat pFormat,
        IntPtr closestMatchFormat); // or outIntPtr??

    int GetMixFormat(out IntPtr deviceFormatPointer);

    int GetDevicePeriod(out long defaultDevicePeriod, out long minimumDevicePeriod);

    int Start();

    int Stop();

    int Reset();

    int SetEventHandle(IntPtr eventHandle);

    [PreserveSig]
    int GetService([In] [MarshalAs(UnmanagedType.LPStruct)] Guid interfaceId,
        [Out] [MarshalAs(UnmanagedType.IUnknown)]
        out object interfacePointer);

    #endregion

    #region IAudioClient2

    void IsOffloadCapable(AudioStreamCategory category, out bool pbOffloadCapable);

    void SetClientProperties([In] IntPtr pProperties);

    void GetBufferSizeLimits(IntPtr pFormat, bool bEventDriven,
        out long phnsMinBufferDuration, out long phnsMaxBufferDuration);

    #endregion

    void GetSharedModeEnginePeriod(
        IntPtr pFormat,
        [Out] out uint pDefaultPeriodInFrames,
        [Out] out uint pFundamentalPeriodInFrames,
        [Out] out uint pMinPeriodInFrames,
        [Out] out uint pMaxPeriodInFrames);

    void GetCurrentSharedModeEnginePeriod(
        [Out] out IntPtr ppFormat,
        [Out] out uint pCurrentPeriodInFrames);

    void InitializeSharedAudioStream(
        [In] uint StreamFlags,
        [In] uint PeriodInFrames,
        [In] IntPtr pFormat,
        [In] [Optional] IntPtr AudioSessionGuid);
}