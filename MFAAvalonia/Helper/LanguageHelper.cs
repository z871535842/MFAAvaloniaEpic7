using Avalonia.Markup.Xaml.MarkupExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace MFAAvalonia.Helper;

public static class LanguageHelper
{
    public static event EventHandler? LanguageChanged;

    public class SupportedLanguage(string key, string name)
    {
        public string? Name => name;
        public string? Key => key;
    }

    public static readonly ObservableCollection<SupportedLanguage> SupportedLanguages =
    [
        new("zh-hans", "简体中文"),
        new("zh-hant", "繁體中文"),
        new("en-us", "English"),
    ];

    public static SupportedLanguage GetLanguage(string key)
    {
        foreach (var lang in SupportedLanguages)
        {
            if (lang.Key == key)
            {
                return lang;
            }
        }
        throw new ArgumentException($"不支持的语言代码: {key}");
    }

    public static void ChangeLanguage(SupportedLanguage language)
    {
        // 设置应用程序的文化
        I18NExtension.Culture = CultureInfo.CreateSpecificCulture(language.Key);

        _currentLanguage = language.Key;

        // 触发语言变化事件
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    // 存储语言的字典
    private static readonly Dictionary<string, Dictionary<string, string>> Langs = new();
    private static string _currentLanguage = SupportedLanguages[0].Key;

    public static void Initialize()
    {
        Console.WriteLine("Initializing LanguageManager...");
        LoadLanguages();
    }

    private static void LoadLanguages()
    {
        var langPath = Path.Combine(AppContext.BaseDirectory, "lang");
        if (Directory.Exists(langPath))
        {
            var langFiles = Directory.GetFiles(langPath, "*.json");
            foreach (string langFile in langFiles)
            {
                var langCode = Path.GetFileNameWithoutExtension(langFile).ToLower();
                if (IsSimplifiedChinese(langCode))
                {
                    langCode = "zh-hans";
                }
                else if (IsTraditionalChinese(langCode))
                {
                    langCode = "zh-hant";
                }
                var jsonContent = File.ReadAllText(langFile);
                var langResources = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
                if (langResources is not null)
                    Langs[langCode] = langResources;
            }
        }
    }

    private static Dictionary<string, string> GetLocalizedStrings()
    {
        return Langs.TryGetValue(_currentLanguage,
            out var dict)
            ? dict
            : new Dictionary<string, string>();
    }

    public static string GetLocalizedString(string? key)
    {
        if (key == null)
            return string.Empty;
        return GetLocalizedStrings().GetValueOrDefault(key, key);
    }


    private static bool IsSimplifiedChinese(string langCode)
    {
        string[] simplifiedPrefixes =
        [
            "zh-hans",
            "zh-cn",
            "zh-sg"
        ];
        foreach (string prefix in simplifiedPrefixes)
        {
            if (langCode.StartsWith(prefix))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsTraditionalChinese(string langCode)
    {
        string[] traditionalPrefixes =
        [
            "zh-hant",
            "zh-tw",
            "zh-hk",
            "zh-mo"
        ];
        foreach (string prefix in traditionalPrefixes)
        {
            if (langCode.StartsWith(prefix))
            {
                return true;
            }
        }
        return false;
    }
}
