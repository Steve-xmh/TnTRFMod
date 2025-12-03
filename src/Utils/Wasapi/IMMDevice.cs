using System.Runtime.InteropServices;

namespace TnTRFMod.Utils.Wasapi;

[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[ComImport]
internal interface IMMDevice
{
    // activationParams is a propvariant
    void Activate(ref Guid id, ClsCtx clsCtx, IntPtr activationParams,
        [MarshalAs(UnmanagedType.IUnknown)] out object? interfacePointer);

    // int OpenPropertyStore(StorageAccessMode stgmAccess, out IPropertyStore properties);
    //     
    // int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
    //     
    // int GetState(out DeviceState state);
}