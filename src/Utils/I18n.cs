using System.Globalization;
using System.Text;
using Tommy;

namespace TnTRFMod.Utils;

using I18nData = Dictionary<string, Dictionary<string, string>>;

public static class I18n
{
    public static readonly string[] FALLBACK_LANGUAGES = ["en", "zhs"];

    private static I18nData _i18nData = new();

    public static DataConst.LanguageType CurrentLanguage
    {
        get
        {
            try
            {
                return CommonObjects.Instance.SaveData.data.LanguageType;
            }
            catch
            {
                try
                {
                    return GetLanguageType(CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
                }
                catch
                {
                    return DataConst.LanguageType.English;
                }
            }
        }
    }

    public static string CurrentLanguageCode => GetLanguageCode(CurrentLanguage);

    public static string GetLanguageCode(DataConst.LanguageType language)
    {
        return language switch
        {
            DataConst.LanguageType.Japanese => "jp",
            DataConst.LanguageType.English => "en",
            DataConst.LanguageType.French => "fr",
            DataConst.LanguageType.Italian => "it",
            DataConst.LanguageType.German => "ge",
            DataConst.LanguageType.Spanish => "sp",
            DataConst.LanguageType.ChineseT => "zht",
            DataConst.LanguageType.ChineseS => "zhs",
            DataConst.LanguageType.Korean => "ko",
            DataConst.LanguageType.Num => "nm",
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
        };
    }

    public static DataConst.LanguageType GetLanguageType(string language)
    {
        return language switch
        {
            "jp" => DataConst.LanguageType.Japanese,
            "en" => DataConst.LanguageType.English,
            "fr" => DataConst.LanguageType.French,
            "it" => DataConst.LanguageType.Italian,
            "ge" => DataConst.LanguageType.German,
            "sp" => DataConst.LanguageType.Spanish,
            "zht" => DataConst.LanguageType.ChineseT,
            "zhs" => DataConst.LanguageType.ChineseS,
            "zh" => DataConst.LanguageType.ChineseS,
            "ko" => DataConst.LanguageType.Korean,
            "nm" => DataConst.LanguageType.Num,
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
        };
    }

    private static void WalkAndAdd(string localeCode, string path, TomlNode node)
    {
        switch (node)
        {
            case TomlTable table:
            {
                foreach (var kvp in table.RawTable) WalkAndAdd(localeCode, $"{path}{kvp.Key}.", kvp.Value);
                break;
            }
            case TomlString str:
            {
                _i18nData[localeCode][path.TrimEnd('.')] = str.Value;
                break;
            }
        }
    }

    public static void Load()
    {
        var data = Encoding.UTF8.GetString(Resources.Locale);
        using var reader = new StringReader(data);
        var i18nTable = TOML.Parse(reader);
        _i18nData = new I18nData();
        foreach (var locale in i18nTable.RawTable)
        {
            _i18nData[locale.Key] = new Dictionary<string, string>();
            WalkAndAdd(locale.Key, "", locale.Value);
        }
    }

    public static I18nResult Get(string key, params object[] args)
    {
        if (_i18nData.TryGetValue(CurrentLanguageCode, out var dict))
            if (dict.TryGetValue(key, out var value))
                return new I18nResult(CurrentLanguage, key, string.Format(value, args));

        foreach (var FALLBACK_LANGUAGE in FALLBACK_LANGUAGES)
            if (_i18nData.TryGetValue(FALLBACK_LANGUAGE, out dict))
                if (dict.TryGetValue(key, out var value))
                {
                    var languageType = GetLanguageType(FALLBACK_LANGUAGE);
                    Logger.Warn(
                        $"Missing I18n key: {key} for language {CurrentLanguageCode}, using fallback language {FALLBACK_LANGUAGE} ({value}).");
                    return new I18nResult(languageType, key, string.Format(value, args));
                }

        Logger.Warn($"Missing I18n key: {key} for language {CurrentLanguageCode}, using key as fallback.");

        return new I18nResult(CurrentLanguage, key, key);
    }

    public struct I18nResult
    {
        public readonly DataConst.LanguageType LanguageType;
        public readonly string Key;
        public readonly string Text;

        internal I18nResult(DataConst.LanguageType languageType, string key, string text)
        {
            LanguageType = languageType;
            Key = key;
            Text = text;
        }

        public static explicit operator DataConst.LanguageType(I18nResult result)
        {
            return result.LanguageType;
        }

        public static explicit operator string(I18nResult result)
        {
            return result.Text;
        }
    }
}