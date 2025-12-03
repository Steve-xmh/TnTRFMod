using Object = Il2CppSystem.Object;

namespace TnTRFMod.Utils;

public static class Il2CppObjectExt
{
    public static T ShadowCopy<T>(this T obj) where T : Object
    {
        return obj.MemberwiseClone().Cast<T>();
    }
}