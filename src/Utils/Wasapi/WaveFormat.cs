using System.Runtime.InteropServices;

namespace TnTRFMod.Utils.Wasapi;

/// <summary>
///     Represents a Wave file format
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 2)]
public class WaveFormat
{
    /// <summary>format type</summary>
    public WaveFormatEncoding waveFormatTag;

    /// <summary>number of channels</summary>
    public short channels;

    /// <summary>sample rate</summary>
    public int sampleRate;

    /// <summary>for buffer estimation</summary>
    public int averageBytesPerSecond;

    /// <summary>block size of data</summary>
    public short blockAlign;

    /// <summary>number of bits per sample of mono data</summary>
    public short bitsPerSample;

    /// <summary>number of following bytes</summary>
    public short extraSize;

    /// <summary>
    ///     Helper function to retrieve a WaveFormat structure from a pointer
    /// </summary>
    /// <param name="pointer">WaveFormat structure</param>
    /// <returns></returns>
    public static WaveFormat MarshalFromPtr(IntPtr pointer)
    {
        var waveFormat = Marshal.PtrToStructure<WaveFormat>(pointer)!;
        if (waveFormat.waveFormatTag == WaveFormatEncoding.Extensible &&
            waveFormat.extraSize >= 22)
            return Marshal.PtrToStructure<WaveFormatExtensible>(pointer)!;

        return waveFormat;
    }

    /// <summary>
    ///     Helper function to retrieve a WaveFormat structure from a pointer
    /// </summary>
    /// <param name="pointer">WaveFormat structure</param>
    /// <returns></returns>
    public IntPtr MarshalToPtr()
    {
        var size = Marshal.SizeOf(this);
        var ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(this, ptr, false);

        return ptr;
    }

    public override string ToString()
    {
        return $"WaveFormatTag: {waveFormatTag}, Channels: {channels}, SampleRate: {sampleRate}, " +
               $"AverageBytesPerSecond: {averageBytesPerSecond}, BlockAlign: {blockAlign}, BitsPerSample: {bitsPerSample}, " +
               $"ExtraSize: {extraSize}";
    }

    public virtual WaveFormat Clone()
    {
        return new WaveFormat
        {
            waveFormatTag = waveFormatTag,
            channels = channels,
            sampleRate = sampleRate,
            averageBytesPerSecond = averageBytesPerSecond,
            blockAlign = blockAlign,
            bitsPerSample = bitsPerSample,
            extraSize = extraSize
        };
    }
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 2)]
public class WaveFormatExtensible : WaveFormat
{
    public short wValidBitsPerSample; // bits of precision, or is wSamplesPerBlock if wBitsPerSample==0
    public int dwChannelMask; // which channels are present in stream
    public Guid subFormat;

    public override string ToString()
    {
        return base.ToString() +
               $", ValidBitsPerSample: {wValidBitsPerSample}, ChannelMask: 0x{dwChannelMask:X}, SubFormat: {subFormat}";
    }

    public override WaveFormat Clone()
    {
        return new WaveFormatExtensible
        {
            waveFormatTag = waveFormatTag,
            channels = channels,
            sampleRate = sampleRate,
            averageBytesPerSecond = averageBytesPerSecond,
            blockAlign = blockAlign,
            bitsPerSample = bitsPerSample,
            extraSize = extraSize,
            wValidBitsPerSample = wValidBitsPerSample,
            dwChannelMask = dwChannelMask,
            subFormat = subFormat
        };
    }
}