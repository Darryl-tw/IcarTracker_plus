using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using System.Security.Claims;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;
using TrackerPlus.Web.Resources;

namespace TrackerPlus.Web.Controllers.Settings;

[Authorize]
[Route("Settings/[action]")]
public class SettingsController : Controller
{
    private readonly IMemberService _memberService;
    private readonly IMemberRepository _memberRepo;
    private readonly ISettingsService _settingsService;
    private readonly IStringLocalizer<SharedResources> _L;

    public SettingsController(IMemberService memberService, IMemberRepository memberRepo,
        ISettingsService settingsService, IStringLocalizer<SharedResources> localizer)
    {
        _memberService = memberService;
        _memberRepo = memberRepo;
        _settingsService = settingsService;
        _L = localizer;
    }

    private int MemberTbKey => int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var k) ? k : 0;
    private bool IsPopupPost => Request.Form["popup"] == "1";

    // ── 系統參數 ────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> SystemParams()
    {
        ViewData["Title"] = _L["Settings_SystemParams"].Value;
        var member = await _memberService.GetMemberAsync(MemberTbKey);
        if (member == null) return NotFound();

        var tzTbKey = await _settingsService.GetMemberTimeZoneTbKeyAsync(MemberTbKey);
        var zones = (await _settingsService.GetTimeZonesAsync()).ToList();

        ViewBag.UserUnit = member.UserUnit;
        ViewBag.TimeZoneTbKey = tzTbKey;
        ViewBag.TimeZones = zones.Select(z => new SelectListItem(z.GmtCode, z.TbKey.ToString())).ToList();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SystemParams(string userUnit, int timeZoneTbKey)
    {
        var result = await _settingsService.UpdateSystemParamsAsync(MemberTbKey, userUnit, timeZoneTbKey);
        if (IsPopupPost)
            return Json(new { success = result.Success, message = result.Message });
        if (result.Success) TempData["Success"] = _L["Settings_SystemParamsSaved"].Value;
        else TempData["Error"] = result.Message;
        return RedirectToAction(nameof(SystemParams));
    }

    // ── 基本資料維護 ─────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        ViewData["Title"] = _L["Settings_Profile"].Value;
        var member = await _memberService.GetMemberAsync(MemberTbKey);
        if (member == null) return NotFound();
        return View(member);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(string cName, string tel, string addr,
        bool isSendEmail, bool isPush, string? newPassword)
    {
        var ok = await _memberRepo.UpdateProfileAsync(MemberTbKey, cName, tel, addr, isSendEmail, isPush);
        if (!ok)
        {
            if (IsPopupPost) return Json(new { success = false, message = _L["Settings_UpdateFailed"].Value });
            TempData["Error"] = _L["Settings_UpdateFailed"].Value;
            return RedirectToAction(nameof(Profile));
        }

        if (!string.IsNullOrWhiteSpace(newPassword))
            await _memberService.ChangePasswordAsync(MemberTbKey, newPassword);

        // 更新 CName claim
        var member = await _memberService.GetMemberAsync(MemberTbKey);
        if (member != null)
        {
            var claims = User.Claims.Where(c => c.Type != "CName").ToList();
            claims.Add(new Claim("CName", member.CName));
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true });
        }

        if (IsPopupPost) return Json(new { success = true });
        TempData["Success"] = _L["Settings_ProfileSaved"].Value;
        return RedirectToAction(nameof(Profile));
    }

    // ── 自定義群組與欄位名稱 ──────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> LabelGroup()
    {
        ViewData["Title"] = _L["Settings_LabelGroup"].Value;
        ViewBag.Fields = (await _settingsService.GetUdFieldsAsync(MemberTbKey)).ToList();
        ViewBag.Labels = (await _settingsService.GetUdLabelsAsync(MemberTbKey)).ToList();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LabelGroup(IFormCollection form)
    {
        var fields = form.Keys
            .Where(k => k.StartsWith("field_"))
            .Select(k => (int.Parse(k[6..]), form[k].ToString()))
            .ToList();

        var labels = form.Keys
            .Where(k => k.StartsWith("label_"))
            .Select(k => (int.Parse(k[6..]), form[k].ToString()))
            .ToList();

        var result = await _settingsService.SaveLabelGroupAsync(MemberTbKey, fields, labels);
        if (form["popup"] == "1")
            return Json(new { success = result.Success, message = result.Message });
        TempData[result.Success ? "Success" : "Error"] = result.Success ? _L["Settings_Saved"].Value : result.Message;
        return RedirectToAction(nameof(LabelGroup));
    }

    // ── 報表欄位 ─────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> ReportFields()
    {
        ViewData["Title"] = _L["Settings_ReportFields"].Value;
        var dto = await _settingsService.GetReportFieldsAsync(MemberTbKey);
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportFields(ReportFieldsDto dto)
    {
        var result = await _settingsService.SaveReportFieldsAsync(MemberTbKey, dto);
        if (IsPopupPost)
            return Json(new { success = result.Success, message = result.Message });
        TempData[result.Success ? "Success" : "Error"] = result.Success ? _L["Settings_ReportFieldsSaved"].Value : result.Message;
        return RedirectToAction(nameof(ReportFields));
    }
}
