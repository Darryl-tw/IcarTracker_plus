using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;
using TrackerPlus.Web.Resources;

namespace TrackerPlus.Web.Controllers;

[Route("Account/[action]")]
public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IMemberService _memberService;
    private readonly IDataProtectionProvider _dataProtection;
    private readonly IStringLocalizer<SharedResources> _L;
    private readonly IGoogleApiKeyService _googleApiKey;
    private readonly IConfiguration _config;

    public AccountController(IAuthService authService, IMemberService memberService,
        IDataProtectionProvider dataProtection, IStringLocalizer<SharedResources> localizer,
        IGoogleApiKeyService googleApiKey, IConfiguration config)
    {
        _authService = authService;
        _dataProtection = dataProtection;
        _memberService = memberService;
        _L = localizer;
        _googleApiKey = googleApiKey;
        _config = config;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Live", "Map");
        ViewBag.ReturnUrl = returnUrl;
        ViewBag.GoogleApiJs = _googleApiKey.GetMapsJavaScriptUrl(Request.Host.Host);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string id, string password, string? returnUrl = null)
    {
        var (success, member, errorMsg) = await _authService.LoginAsync(id, password);
        if (!success || member == null)
        {
            ViewBag.Error = errorMsg;
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.GoogleApiJs = _googleApiKey.GetMapsJavaScriptUrl(Request.Host.Host);
            return View();
        }

        await SignInMemberAsync(member);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Live", "Map");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        if (string.Equals(provider, "Apple", StringComparison.OrdinalIgnoreCase))
        {
            var callbackUrl = Url.Action("AppleLoginCallback", "Account", null, Request.Scheme);
            var appleProxyUrl = _config["AppSettings:OAuth:AppleRedirectURL"] ?? "https://OAuth2.traceez.com/appleid.aspx";
            var clientId = _config["AppSettings:OAuth:AppleClientId"] ?? "";
            return Redirect($"{appleProxyUrl}?clientId={Uri.EscapeDataString(clientId)}&redirectUri={Uri.EscapeDataString(callbackUrl ?? "")}");
        }

        var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, provider);
    }

    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
    {
        var result = await HttpContext.AuthenticateAsync("ExternalCookie");
        if (!result.Succeeded)
        {
            ViewBag.Error = _L["Login_ExternalFailed"].Value;
            ViewBag.GoogleApiJs = _googleApiKey.GetMapsJavaScriptUrl(Request.Host.Host);
            return View("Login");
        }

        var email = result.Principal?.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            ViewBag.Error = _L["Login_ExternalNoEmail"].Value;
            ViewBag.GoogleApiJs = _googleApiKey.GetMapsJavaScriptUrl(Request.Host.Host);
            return View("Login");
        }

        var (success, member, errorMsg) = await _authService.LoginByEmailAsync(email);
        if (!success || member == null)
        {
            ViewBag.Error = errorMsg;
            ViewBag.GoogleApiJs = _googleApiKey.GetMapsJavaScriptUrl(Request.Host.Host);
            return View("Login");
        }

        await HttpContext.SignOutAsync("ExternalCookie");
        await SignInMemberAsync(member);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Live", "Map");
    }

    [HttpGet]
    public IActionResult AppleLoginCallback(string? token = null, string? email = null)
    {
        // Apple proxy sends back token/email — handled similarly to ExternalLoginCallback
        // Full implementation depends on proxy response format
        ViewBag.Error = _L["Login_ExternalFailed"].Value;
        ViewBag.GoogleApiJs = _googleApiKey.GetMapsJavaScriptUrl(Request.Host.Host);
        return View("Login");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(Member member, string password)
    {
        if (!ModelState.IsValid)
            return View(member);

        var result = await _memberService.RegisterMemberAsync(member, password);
        if (!result.Success)
        {
            ViewBag.Error = result.Message;
            return View(member);
        }

        ViewBag.Success = _L["Register_Success"].Value;
        return View();
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ViewBag.Error = _L["ForgotPassword_EmailRequired"].Value;
            return View();
        }
        await _authService.SendPasswordResetEmailAsync(email);
        ViewBag.Success = _L["ForgotPassword_Sent"].Value;
        return View();
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    // ── 管理員前台預覽：解碼 token 後以對應會員身份簽入，跳轉至即時地圖 ────
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> AdminPreview(string t)
    {
        if (string.IsNullOrWhiteSpace(t))
            return BadRequest();
        try
        {
            var protector = _dataProtection.CreateProtector("AdminPreview");
            var payload = protector.Unprotect(t);
            var parts = payload.Split(':');
            if (parts.Length != 3) return BadRequest();

            var trackerTbKey = int.Parse(parts[0]);
            var memberTbKey  = int.Parse(parts[1]);
            var expiry       = long.Parse(parts[2]);

            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiry)
                return Content($"<html><body><p>{_L["Account_AdminPreviewExpired"].Value}</p></body></html>", "text/html");

            List<Claim> claims;
            if (memberTbKey > 0)
            {
                var member = await _memberService.GetMemberAsync(memberTbKey);
                if (member == null) return NotFound();
                claims = new List<Claim>
                {
                    new(ClaimTypes.Name, member.ID),
                    new(ClaimTypes.NameIdentifier, member.TbKey.ToString()),
                    new("Timezoom", member.Timezoom.ToString()),
                    new("CName", member.CName),
                    new("UserLanguage", member.UserLanguage),
                    new("AdminPreviewTbKey", trackerTbKey.ToString())
                };
            }
            else
            {
                claims = new List<Claim>
                {
                    new(ClaimTypes.Name, "admin-preview"),
                    new(ClaimTypes.NameIdentifier, "0"),
                    new("Timezoom", "480"),
                    new("CName", "Admin Preview"),
                    new("UserLanguage", "zh-TW"),
                    new("AdminPreviewTbKey", trackerTbKey.ToString())
                };
            }

            var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties { IsPersistent = false });

            return Redirect($"/Map/Live?focus={trackerTbKey}");
        }
        catch
        {
            return BadRequest();
        }
    }

    private async Task SignInMemberAsync(Member member)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, member.ID),
            new(ClaimTypes.NameIdentifier, member.TbKey.ToString()),
            new("Timezoom", member.Timezoom.ToString()),
            new("CName", member.CName),
            new("UserLanguage", member.UserLanguage)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true });

        // Only set culture cookie if user hasn't already selected one (preserve pre-login language selection)
        var existingCulture = Request.Cookies[CookieRequestCultureProvider.DefaultCookieName];
        if (string.IsNullOrEmpty(existingCulture))
        {
            var culture = LocalizationCulture.ToCultureName(member.UserLanguage);
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });
        }
    }
}
