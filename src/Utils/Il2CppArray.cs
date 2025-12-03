using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace TnTRFMod.Utils;

public static class Il2CppArray
{
    public static Il2CppReferenceArray<T> ResizeArray<T>(this Il2CppReferenceArray<T> array, int newSize,
        Func<int, T> creator)
        where T : Il2CppObjectBase
    {
        var oldSize = array.Length;
        if (oldSize == newSize) return array;
        T[] original = array;
        Array.Resize(ref original, newSize);
        for (var i = oldSize; i < newSize; i++)
            original[i] = creator(i);
        return original;
    }

    public static Il2CppReferenceArray<T> ResizeArray<T>(this Il2CppReferenceArray<T> array, int newSize)
        where T : Il2CppObjectBase
    {
        var oldSize = array.Length;
        if (oldSize == newSize) return array;
        T[] original = array;
        Array.Resize(ref original, newSize);
        for (var i = oldSize; i < newSize; i++)
            original[i] = null;
        return original;
    }
}