#if BEPINEX
using BepInEx.Logging;
#endif

#if MELONLOADER
using MelonLoader;
#endif

namespace TnTRFMod.Utils;

#if BEPINEX
public static class Logger
{
    internal static ManualLogSource _inner;

    public static void Info(object data)
    {
        _inner.LogInfo(data);
    }

    public static void Message(object data)
    {
        _inner.LogMessage(data);
    }

    public static void Warn(object data)
    {
        _inner.LogWarning(data);
    }

    public static void Error(object data)
    {
        _inner.LogError(data);
    }

    public static void Fatal(object data)
    {
        _inner.LogFatal(data);
    }
}
#endif
#if MELONLOADER
public static class Logger
{
    internal static MelonLogger.Instance _inner;

    public static void Info(object data)
    {
        _inner.Msg(data);
    }

    public static void Message(object data)
    {
        _inner.Msg(data);
    }

    public static void Warn(object data)
    {
        _inner.Warning(data);
    }

    public static void Error(object data)
    {
        _inner.Error(data);
    }

    public static void Fatal(object data)
    {
        _inner.Error(data);
    }
}
#endif