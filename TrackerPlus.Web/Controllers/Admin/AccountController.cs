using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Services;

namespace TrackerPlus.Web.Controllers.Admin;

[Area("Admin")]
public class AccountController : Controller
{
    private const string AdminScheme = "AdminCookie";
    private readonly IAuthService _authService;
    private readonly AdminAuthSettings _adminAuth;

    public AccountController(IAuthService authService, IOptions<AdminAuthSettings> adminAuth)
    {
        _authService = authService;
        _adminAuth = adminAuth.Value;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        var adminAuth = await HttpContext.AuthenticateAsync(AdminScheme);
        if (adminAuth.Succeeded)
            return RedirectToLocal(returnUrl);

        ViewBag.ReturnUrl = returnUrl;
        ViewBag.Account = Request.Cookies["TrackerPlus.Admin.Account"];
        ViewBag.RememberMe = Request.Cookies["TrackerPlus.Admin.Remember"] == "1";
        ViewBag.IdleLogout = Request.Query["idle"] == "1";
        return View("~/Views/Shared/_AdminLoginPage.cshtml");
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        string account,
        string password,
        bool rememberMe = false,
        string? returnUrl = null)
    {
        var (success, errorMsg) = await _authService.AdminLoginAsync(account, password);
        if (!success)
        {
            ViewBag.Error = errorMsg;
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.Account = account;
            ViewBag.RememberMe = rememberMe;
            return View("~/Views/Shared/_AdminLoginPage.cshtml");
        }

        var minutes = rememberMe ? _adminAuth.RememberMeMinutes : _adminAuth.SessionMinutes;
        var now = DateTimeOffset.UtcNow;
        var props = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            IssuedUtc = now,
            ExpiresUtc = now.AddMinutes(minutes)
        };
        props.Items["LastActivityUtc"] = now.ToString("o", CultureInfo.InvariantCulture);
        props.Items["RememberMe"] = rememberMe ? "1" : "0";

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, account),
            new(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, AdminScheme);
        await HttpContext.SignInAsync(AdminScheme, new ClaimsPrincipal(identity), props);

        Response.Cookies.Append("TrackerPlus.Admin.Account", account,
            new CookieOptions
            {
                Expires = now.AddDays(30),
                HttpOnly = false,
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });
        Response.Cookies.Append("TrackerPlus.Admin.Remember", rememberMe ? "1" : "0",
            new CookieOptions
            {
                Expires = now.AddDays(30),
                HttpOnly = false,
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [HttpGet]
    public async Task<IActionResult> Logout(string? reason = null)
    {
        await HttpContext.SignOutAsync(AdminScheme);
        Response.Cookies.Delete("TrackerPlus.Admin.Account");
        Response.Cookies.Delete("TrackerPlus.Admin.Remember");
        if (string.Equals(reason, "idle", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Login), new { area = "Admin", idle = 1 });
        return RedirectToAction(nameof(Login), new { area = "Admin" });
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Tracker", new { area = "Admin" });
    }

}
