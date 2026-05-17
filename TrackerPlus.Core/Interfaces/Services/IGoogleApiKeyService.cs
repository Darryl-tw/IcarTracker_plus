using TrackerPlus.Core.Common;

namespace TrackerPlus.Core.Interfaces.Services;

/// <summary>依主機與用途解析 Google API 金鑰（對應舊站 TrackerFUN.GetGoogleAPIKey）</summary>
public interface IGoogleApiKeyService
{
    /// <summary>
    /// localhost / 127.0.0.1 使用 DeveloperUse；其餘依 <paramref name="keyType"/> 使用 JS / URL / LBS。
    /// </summary>
    string GetApiKey(string? hostName, GoogleApiKeyType keyType);

    /// <summary>Maps JavaScript API 載入網址（不含 callback 參數）</summary>
    string GetMapsJavaScriptUrl(string? hostName);
}
