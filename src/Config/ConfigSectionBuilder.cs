using UnityEngine.InputSystem;

namespace TnTRFMod.Config;

/// <summary>
/// 流式配置节构建器，用于优雅地分组定义配置项。
/// 使用方式：
/// <code>
/// ConfigSectionBuilder.Section("General", s => {
///     enableMod = s.Bool("Enabled", "config.Enabled", true);
///     maxCount = s.Int("MaxCount", "config.MaxCount", 5);
/// });
/// </code>
/// </summary>
public class ConfigSectionBuilder
{
    private readonly string _section;

    private ConfigSectionBuilder(string section)
    {
        _section = section;
    }

    /// <summary>
    /// 创建一个配置节，在其中定义该节下的所有配置项。
    /// </summary>
    public static void Section(string name, Action<ConfigSectionBuilder> configure)
    {
        var builder = new ConfigSectionBuilder(name);
        configure(builder);
    }

    public ConfigEntry<bool> Bool(string key, string description, bool defaultValue = default)
    {
        return ConfigEntry<bool>.Register(_section, key, description, defaultValue);
    }

    public ConfigEntry<int> Int(string key, string description, int defaultValue = default)
    {
        return ConfigEntry<int>.Register(_section, key, description, defaultValue);
    }

    public ConfigEntry<float> Float(string key, string description, float defaultValue = default)
    {
        return ConfigEntry<float>.Register(_section, key, description, defaultValue);
    }

    public ConfigEntry<double> Double(string key, string description, double defaultValue = default)
    {
        return ConfigEntry<double>.Register(_section, key, description, defaultValue);
    }

    public ConfigEntry<string> String(string key, string description, string defaultValue = default!)
    {
        return ConfigEntry<string>.Register(_section, key, description, defaultValue);
    }

    public ConfigEntry<uint> UInt(string key, string description, uint defaultValue = default)
    {
        return ConfigEntry<uint>.Register(_section, key, description, defaultValue);
    }

    public KeyBindingConfigEntry KeyBinding(string key, string description, Key defaultValue = default)
    {
        return KeyBindingConfigEntry.Register(_section, key, description, defaultValue);
    }
}