using System.Runtime.InteropServices;

namespace TnTRFMod.Utils.Wasapi;

[ComImport]
[Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
internal class MMDeviceEnumeratorComObject
{
}

[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[ComImport]
internal interface IMMDeviceEnumerator
{
    int EnumAudioEndpoints(
        DataFlow dataFlow,
        DeviceState stateMask,
        out IMMDeviceCollection devices);

    void GetDefaultAudioEndpoint(DataFlow dataFlow, Role role, out IMMDevice endpoint);

    // int GetDevice(string id, out IMMDevice deviceName);
    //     
    // int RegisterEndpointNotificationCallback(IMMNotificationClient client);
    //     
    // int UnregisterEndpointNotificationCallback(IMMNotificationClient client);
}

/// <summary>
///     The EDataFlow enumeration defines constants that indicate the direction
///     in which audio data flows between an audio endpoint device and an application
/// </summary>
public enum DataFlow
{
    /// <summary>
    ///     Audio rendering stream.
    ///     Audio data flows from the application to the audio endpoint device, which renders the stream.
    /// </summary>
    Render,

    /// <summary>
    ///     Audio capture stream. Audio data flows from the audio endpoint device that captures the stream,
    ///     to the application
    /// </summary>
    Capture,

    /// <summary>
    ///     Audio rendering or capture stream. Audio data can flow either from the application to the audio
    ///     endpoint device, or from the audio endpoint device to the application.
    /// </summary>
    All
}

/// <summary>
///     The ERole enumeration defines constants that indicate the role
///     that the system has assigned to an audio endpoint device
/// </summary>
public enum Role
{
    /// <summary>
    ///     Games, system notification sounds, and voice commands.
    /// </summary>
    Console,

    /// <summary>
    ///     Music, movies, narration, and live music recording
    /// </summary>
    Multimedia,

    /// <summary>
    ///     Voice communications (talking to another person).
    /// </summary>
    Communications
}

/// <summary>Device State</summary>
[Flags]
public enum DeviceState
{
    /// <summary>DEVICE_STATE_ACTIVE</summary>
    Active = 1,

    /// <summary>DEVICE_STATE_DISABLED</summary>
    Disabled = 2,

    /// <summary>DEVICE_STATE_NOTPRESENT</summary>
    NotPresent = 4,

    /// <summary>DEVICE_STATE_UNPLUGGED</summary>
    Unplugged = 8,

    /// <summary>DEVICE_STATEMASK_ALL</summary>
    All = Unplugged | NotPresent | Disabled | Active // 0x0000000F
}

[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[ComImport]
internal interface IMMDeviceCollection
{
    int GetCount(out int numDevices);

    int Item(int deviceNumber, out IMMDevice device);
}