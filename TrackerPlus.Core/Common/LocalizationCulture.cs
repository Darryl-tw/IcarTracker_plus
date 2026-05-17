using System.Globalization;

namespace TrackerPlus.Core.Common;

/// <summary>舊版 UserLanguage (CHT/CHS/ENG) 與 .NET Culture 對照</summary>
public static class LocalizationCulture
{
    public const string DefaultCulture = "zh-TW";

    public static readonly string[] SupportedCultures = ["zh-TW", "zh-CN", "en-US"];

    public static string ToCultureName(string? userLanguage)
    {
        return (userLanguage?.Trim().ToUpperInvariant()) switch
        {
            "CHT" => "zh-TW",
            "CHS" => "zh-CN",
            "ENG" or "EN" => "en-US",
            _ when !string.IsNullOrWhiteSpace(userLanguage) && SupportedCultures.Contains(userLanguage, StringComparer.OrdinalIgnoreCase)
                => SupportedCultures.First(c => c.Equals(userLanguage, StringComparison.OrdinalIgnoreCase)),
            _ => DefaultCulture
        };
    }

    public static string ToUserLanguage(string? cultureName)
    {
        return cultureName?.Trim() switch
        {
            "zh-TW" or "zh-CHT" => "CHT",
            "zh-CN" or "zh-Hans" => "CHS",
            "en-US" or "en" => "ENG",
            _ => "CHT"
        };
    }

    public static CultureInfo GetCultureInfo(string? cultureOrUserLanguage)
    {
        var name = cultureOrUserLanguage?.Contains('-') == true
            ? cultureOrUserLanguage
            : ToCultureName(cultureOrUserLanguage);
        try
        {
            return CultureInfo.GetCultureInfo(name);
        }
        catch
        {
            return CultureInfo.GetCultureInfo(DefaultCulture);
        }
    }
}
