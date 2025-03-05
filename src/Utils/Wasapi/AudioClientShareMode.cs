// From NAudio.CoreAudioApi

namespace TnTRFMod.Utils.Wasapi;

/// <summary>
///     AUDCLNT_SHAREMODE
/// </summary>
internal enum AudioClientShareMode
{
    /// <summary>
    ///     AUDCLNT_SHAREMODE_SHARED,
    /// </summary>
    Shared,

    /// <summary>
    ///     AUDCLNT_SHAREMODE_EXCLUSIVE
    /// </summary>
    Exclusive
}