using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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

    public AccountController(IAuthService authService, IMemberService memberService)
    {
        _authService = authService;
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
}
