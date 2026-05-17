using Microsoft.AspNetCore.Localization;
using TrackerPlus.Core.Common;

namespace TrackerPlus.Web.Localization;

/// <summary>已登入會員依 Claim UserLanguage 決定語系（次於 Cookie）</summary>
public class UserLanguageRequestCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var lang = httpContext.User?.FindFirst("UserLanguage")?.Value;
        if (string.IsNullOrWhiteSpace(lang))
            return NullProviderCultureResult;

        var culture = LocalizationCulture.ToCultureName(lang);
        return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(culture, culture));
    }
}
