using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Web.Controllers;

[Route("Account/[action]")]
public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IMemberService _memberService;
    private readonly IDataProtectionProvider _dataProtection;

    public AccountController(IAuthService authService, IMemberService memberService,
        IDataProtectionProvider dataProtection)
    {
        _authService = authService;
        _dataProtection = dataProtection;
        _memberService = memberService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Live", "Map");
        ViewBag.ReturnUrl = returnUrl;
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
            return View();
        }

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

        var culture = LocalizationCulture.ToCultureName(member.UserLanguage);
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Live", "Map");
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

        ViewBag.Success = "註冊成功，請等待帳號審核啟用。";
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
            ViewBag.Error = "請輸入電子郵件";
            return View();
        }
        await _authService.SendPasswordResetEmailAsync(email);
        ViewBag.Success = "若此信箱已註冊，重設密碼信件已寄出，請檢查您的信箱。";
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
                return Content("<html><body><p>預覽連結已過期，請在後台重新點擊「前台」按鈕。</p></body></html>", "text/html");

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
                // 未綁定裝置：建立只含該裝置的預覽身份
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
}
