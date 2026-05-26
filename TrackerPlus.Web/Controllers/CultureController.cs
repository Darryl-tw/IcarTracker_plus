using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Services;

namespace TrackerPlus.Web.Controllers;

[Route("Culture/[action]")]
public class CultureController : Controller
{
    private readonly IMemberService? _memberService;

    public CultureController(IMemberService memberService)
    {
        _memberService = memberService;
    }

    /// <summary>切換語系（GET，登入頁也可用）</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Set(string culture, string? returnUrl = null)
    {
        if (!LocalizationCulture.SupportedCultures.Contains(culture, StringComparer.OrdinalIgnoreCase))
            culture = LocalizationCulture.DefaultCulture;

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                HttpOnly = false
            });

        if (User.Identity?.IsAuthenticated == true
            && int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var memberTbKey)
            && _memberService != null)
        {
            await _memberService.UpdateMemberLanguageAsync(memberTbKey, LocalizationCulture.ToUserLanguage(culture));
        }

        returnUrl ??= Request.Headers.Referer.ToString();
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        if (User.Identity?.IsAuthenticated == true)
        {
            if (Request.Path.StartsWithSegments("/Admin") || (returnUrl?.Contains("/Admin", StringComparison.OrdinalIgnoreCase) ?? false))
                return RedirectToAction("Index", "Tracker", new { area = "Admin" });
            return RedirectToAction("Index", "Tracker");
        }

        if (!string.IsNullOrEmpty(returnUrl) && returnUrl.Contains("/Admin", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction("Login", "Account", new { area = "Admin" });

        return RedirectToAction("Login", "Account");
    }
}
