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
        var waveFormat = Marshal.PtrToStructure<WaveFormat>(pointer);

        return waveFormat;
    }

    public override string ToString()
    {
        return $"WaveFormatTag: {waveFormatTag}, Channels: {channels}, SampleRate: {sampleRate}, " +
               $"AverageBytesPerSecond: {averageBytesPerSecond}, BlockAlign: {blockAlign}, BitsPerSample: {bitsPerSample}, " +
               $"ExtraSize: {extraSize}";
    }
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 2)]
public class WaveFormatExtensible : WaveFormat
{
    public short wValidBitsPerSample; // bits of precision, or is wSamplesPerBlock if wBitsPerSample==0
    public int dwChannelMask; // which channels are present in stream
    public Guid subFormat;
}