#if BEPINEX
using TnTRFMod.Loader;
#endif

#if MELONLOADER
using MelonLoader;
#endif

namespace TnTRFMod.Config;

public class ConfigEntry<T>
{
#if BEPINEX
    private BepInEx.Configuration.ConfigEntry<T> inner;

    public static ConfigEntry<TT> Register<TT>(string section, string key, string description = "",
        TT defaultValue = default)
    {
        return new ConfigEntry<TT>
        {
            inner = BepInExPlugin.Instance.Config.Bind(section, key, defaultValue, description)
        };
    }

    public T Value => inner.Value;
#endif

#if MELONLOADER
    private MelonPreferences_Entry<T> inner;

    public static ConfigEntry<TT> Register<TT>(string section, string key, string description = "",
        TT defaultValue = default)
    {
        var category = MelonPreferences.GetCategory(section) ?? MelonPreferences.CreateCategory(section);

        return new ConfigEntry<TT>
        {
            inner = category.CreateEntry(key, defaultValue, key, description)
        };
    }

    public T Value => inner.Value;
#endif
}

public static class ConfigEntry
{
    public static ConfigEntry<T> Register<T>(string section, string key, string description = "",
        T defaultValue = default)
    {
        return ConfigEntry<T>.Register(section, key, description, defaultValue);
    }
}