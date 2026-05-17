using Microsoft.Extensions.Options;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Services;

namespace TrackerPlus.Services.Services;

public class GoogleApiKeyService : IGoogleApiKeyService
{
    private readonly AppSettings _appSettings;

    public GoogleApiKeyService(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;
    }

    public string GetApiKey(string? hostName, GoogleApiKeyType keyType)
    {
        if (IsLocalHost(hostName))
            return _appSettings.GoogleApi.DeveloperUse ?? string.Empty;

        return keyType switch
        {
            GoogleApiKeyType.Js => GetProductionJsKey(),
            GoogleApiKeyType.Url => _appSettings.GoogleApi.URL ?? string.Empty,
            GoogleApiKeyType.Lbs => _appSettings.GoogleApi.LBS ?? string.Empty,
            _ => string.Empty
        };
    }

    public string GetMapsJavaScriptUrl(string? hostName)
    {
        var key = GetApiKey(hostName, GoogleApiKeyType.Js);
        return string.IsNullOrEmpty(key)
            ? string.Empty
            : $"https://maps.googleapis.com/maps/api/js?key={Uri.EscapeDataString(key)}";
    }

    internal static bool IsLocalHost(string? hostName)
    {
        if (string.IsNullOrWhiteSpace(hostName))
            return false;

        return hostName.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || hostName.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || hostName.Equals("::1", StringComparison.OrdinalIgnoreCase)
            || hostName.Equals("[::1]", StringComparison.OrdinalIgnoreCase);
    }

    private string GetProductionJsKey() => ExtractKeyFromJsSetting(_appSettings.GoogleApi.JS);

    private static string ExtractKeyFromJsSetting(string? jsSetting)
    {
        if (string.IsNullOrWhiteSpace(jsSetting))
            return string.Empty;

        if (!jsSetting.Contains("maps.googleapis.com", StringComparison.OrdinalIgnoreCase)
            && !jsSetting.Contains("key=", StringComparison.OrdinalIgnoreCase))
            return jsSetting.Trim();

        if (Uri.TryCreate(jsSetting, UriKind.Absolute, out var uri))
        {
            var query = uri.Query.TrimStart('?');
            foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                if (part.StartsWith("key=", StringComparison.OrdinalIgnoreCase))
                    return Uri.UnescapeDataString(part[4..]);
            }
        }

        const string keyParam = "key=";
        var idx = jsSetting.IndexOf(keyParam, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return jsSetting.Trim();

        var start = idx + keyParam.Length;
        var end = jsSetting.IndexOf('&', start);
        return end < 0
            ? jsSetting[start..].Trim()
            : jsSetting[start..end].Trim();
    }
}
