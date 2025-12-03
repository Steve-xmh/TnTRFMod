using TnTRFMod.Utils;
using Tommy;

namespace TnTRFMod.Config;

public class ConfigEntry<T>
{
    public static readonly ConfigEntry<T> Noop = new()
    {
        key = "",
        defaultValue = default,
        section = ""
    };

    private T defaultValue;
    private string key;
    private string section;

    public T Value
    {
        get
        {
            TomlNode section;
            if (ConfigEntry.loadedConfig.HasKey(this.section))
            {
                section = ConfigEntry.loadedConfig[this.section];
                if (section is TomlTable table && table.HasKey(key))
                {
                    var value = table[key];
                    switch (value)
                    {
                        case TomlString str:
                            return (T)Convert.ChangeType(str.Value, typeof(T));
                        case TomlBoolean boolean:
                            return (T)Convert.ChangeType(boolean.Value, typeof(T));
                        case TomlInteger integer:
                            return (T)Convert.ChangeType(integer.Value, typeof(T));
                        case TomlFloat floating:
                            return (T)Convert.ChangeType(floating.Value, typeof(T));
                    }
                }
            }

            if (!ConfigEntry.defaultConfig.HasKey(this.section)) return defaultValue;

            section = ConfigEntry.defaultConfig[this.section];
            if (section is not TomlTable defTable || !defTable.HasKey(key)) return defaultValue;
            {
                var value = defTable[key];

                return value switch
                {
                    TomlString str => (T)Convert.ChangeType(str.Value, typeof(T)),
                    TomlBoolean boolean => (T)Convert.ChangeType(boolean.Value, typeof(T)),
                    TomlInteger integer => (T)Convert.ChangeType(integer.Value, typeof(T)),
                    TomlFloat floating => (T)Convert.ChangeType(floating.Value, typeof(T)),
                    _ => defaultValue
                };
            }
        }
    }

    public static ConfigEntry<TT> Register<TT>(string section, string key, string description = "",
        TT defaultValue = default!)
    {
        var defaultConfig = ConfigEntry.defaultConfig;
        if (!defaultConfig.HasKey(section))
            defaultConfig[section] = new TomlTable();

        var descriptionText = I18n.Get(description);

        defaultConfig[section][key] = defaultValue switch
        {
            string value => new TomlString { Comment = descriptionText, Value = value },
            bool value => new TomlBoolean { Comment = descriptionText, Value = value },
            short value => new TomlInteger { Comment = descriptionText, Value = value },
            ushort value => new TomlInteger { Comment = descriptionText, Value = value },
            int value => new TomlInteger { Comment = descriptionText, Value = value },
            uint value => new TomlInteger { Comment = descriptionText, Value = value },
            float value => new TomlFloat { Comment = descriptionText, Value = value },
            double value => new TomlFloat { Comment = descriptionText, Value = value },
            long value => new TomlInteger { Comment = descriptionText, Value = value },
            ulong value => new TomlInteger { Comment = descriptionText, Value = (long)value },
            _ => throw new ArgumentException(
                $"Unsupported type {typeof(TT)} for config entry '{section}.{key}'")
        };

        return new ConfigEntry<TT>
        {
            section = section,
            key = key,
            defaultValue = defaultValue
        };
    }
}

public static class ConfigEntry
{
    public static readonly TomlTable defaultConfig = new();
    public static TomlTable loadedConfig = new();

    public static bool IsFirstConfig = true;

    private static FileSystemWatcher? configFileWatcher;

    public static string ConfigFilePath => Path.Combine(TnTrfMod.Dir, "config.toml");
    public static string ExampleConfigFilePath => Path.Combine(TnTrfMod.Dir, "config.example.toml");

    public static ConfigEntry<T> Register<T>(string section, string key, string description = "",
        T defaultValue = default)
    {
        return ConfigEntry<T>.Register(section, key, description, defaultValue);
    }

    public static void Load()
    {
        if (!Directory.Exists(TnTrfMod.Dir))
            Directory.CreateDirectory(TnTrfMod.Dir);

        configFileWatcher = new FileSystemWatcher(TnTrfMod.Dir, "config.toml")
        {
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        configFileWatcher.Changed += OnConfigFileChanged;
        ExportDefaultConfig();

        if (File.Exists(ConfigFilePath))
        {
            try
            {
                using var reader = File.Open(ConfigFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var streamReader = new StreamReader(reader);
                loadedConfig = TOML.Parse(streamReader);
                IsFirstConfig = false;
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load config file: {e}");
                loadedConfig = defaultConfig;
            }

            return;
        }

        using var writer = new StreamWriter(ConfigFilePath);
        defaultConfig.WriteTo(writer);
    }

    private static void ExportDefaultConfig()
    {
        using var writer = new StreamWriter(ExampleConfigFilePath);
        defaultConfig.WriteTo(writer);
    }

    private static void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!File.Exists(ConfigFilePath)) return;
        try
        {
            using var reader = File.Open(ConfigFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var streamReader = new StreamReader(reader);
            loadedConfig = TOML.Parse(streamReader);
            IsFirstConfig = false;
            Logger.Info("Config file reloaded successfully.");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to reload config file: {ex}");
            loadedConfig = defaultConfig;
        }
    }
}