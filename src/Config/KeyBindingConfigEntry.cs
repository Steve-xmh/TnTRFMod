using TnTRFMod.Utils;
using Tommy;
using UnityEngine.InputSystem;

namespace TnTRFMod.Config;

public class KeyBindingConfigEntry
{
    internal static readonly TomlTable defaultConfig = new();
    internal static TomlTable loadedConfig = new();

    public static bool IsFirstConfig = true;

    public static readonly KeyBindingConfigEntry Noop = new()
    {
        key = "",
        defaultValue = Key.None,
        section = ""
    };

    private static FileSystemWatcher? configFileWatcher;

    private Key defaultValue;

    private string key = "";
    private string section = "";

    public Key Value
    {
        get
        {
            TomlNode thisSection;
            if (loadedConfig.HasKey(section))
            {
                thisSection = loadedConfig[section];
                if (thisSection is TomlTable table && table.HasKey(key))
                {
                    var value = table[key];
                    switch (value)
                    {
                        case TomlString str:
                            try
                            {
                                return Enum.Parse<Key>(str.Value);
                            }
                            catch (Exception)
                            {
                                break;
                            }
                    }
                }
            }

            if (!defaultConfig.HasKey(section)) return defaultValue;

            thisSection = defaultConfig[section];
            if (thisSection is not TomlTable defTable || !defTable.HasKey(key)) return defaultValue;
            {
                var value = defTable[key];

                try
                {
                    return value switch
                    {
                        TomlString str => Enum.Parse<Key>(str.Value),
                        _ => defaultValue
                    };
                }
                catch (Exception)
                {
                    return defaultValue;
                }
            }
        }
    }

    public static string ConfigFilePath => Path.Combine(TnTrfMod.Dir, "keybindings.toml");
    public static string ExampleConfigFilePath => Path.Combine(TnTrfMod.Dir, "keybindings.example.toml");

    public static KeyBindingConfigEntry Register(string section, string key, string description = "",
        Key defaultValue = default!)
    {
        if (!defaultConfig.HasKey(section))
            defaultConfig[section] = new TomlTable();

        var descriptionText = I18n.Get(description);

        defaultConfig[section][key] = new TomlString { Comment = descriptionText, Value = Enum.GetName(defaultValue) };

        return new KeyBindingConfigEntry
        {
            section = section,
            key = key,
            defaultValue = defaultValue
        };
    }

    public static void Load()
    {
        if (!Directory.Exists(TnTrfMod.Dir))
            Directory.CreateDirectory(TnTrfMod.Dir);

        configFileWatcher = new FileSystemWatcher(TnTrfMod.Dir, "keybindings.toml")
        {
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        configFileWatcher.Changed += OnConfigFileChanged;
        defaultConfig.Comment = "可用的键盘按键列表参考：\n";
        var w = 0;
        foreach (var name in Enum.GetNames<Key>())
        {
            if (w + name.Length > 70)
            {
                defaultConfig.Comment += "\n";
                w = name.Length;
            }
            else
            {
                if (w > 0) defaultConfig.Comment += ", ";
                w += name.Length + 2;
            }

            defaultConfig.Comment += name;
        }

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
                Logger.Error($"Failed to load key binding config file: {e}");
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
            Logger.Info("Key binding config file reloaded successfully.");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to reload key binding config file: {ex}");
            loadedConfig = defaultConfig;
        }
    }
}