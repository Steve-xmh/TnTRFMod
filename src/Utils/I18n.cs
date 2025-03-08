using System.Text;
using System.Text.Json;

namespace TnTRFMod.Utils;

using I18nData = Dictionary<string, Dictionary<string, string>>;

public static class I18n
{
    public const string FALLBACK_LANGUAGE = "zhs";

    private static I18nData _i18nData = new();
    public static DataConst.LanguageType CurrentLanguage => CommonObjects.Instance.SaveData.data.LanguageType;
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

    public static void Load()
    {
        _i18nData = JsonSerializer.Deserialize<I18nData>(Encoding.UTF8.GetString(Resources.Locale));
    }

    public static string Get(string key, params object[] args)
    {
        if (_i18nData.TryGetValue(CurrentLanguageCode, out var dict))
            if (dict.TryGetValue(key, out var value))
                return string.Format(value, args);

        if (_i18nData.TryGetValue(FALLBACK_LANGUAGE, out dict))
            if (dict.TryGetValue(key, out var value))
                return string.Format(value, args);

        return key;
    }
}