using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Web.Controllers.Admin.Member;

public class MemberController : AdminBaseController
{
    private readonly IMemberService _memberService;
    private readonly IAuthService _authService;
    private readonly IDataProtectionProvider _dataProtection;

    public MemberController(IMemberService memberService, IAuthService authService, IDataProtectionProvider dataProtection)
    {
        _memberService = memberService;
        _authService = authService;
        _dataProtection = dataProtection;
    }

    public async Task<IActionResult> Index(string? keyword, string? status, int page = 1)
    {
        var filter = new QueryFilter { Keyword = keyword, Status = status, PageIndex = page, PageSize = 100 };
        var result = await _memberService.GetMembersAsync(filter);
        ViewBag.Filter = filter;
        return View(result);
    }

    // ── 產生前台預覽 Token（5 分鐘有效，讓管理員免登入以該會員身份查看前台） ──
    [HttpGet]
    public IActionResult GetPreviewToken(int memberTbKey)
    {
        if (memberTbKey <= 0)
            return Json(new { success = false, message = L["Admin_Error_NoMemberSelected"].Value });
        var protector = _dataProtection.CreateProtector("AdminPreview");
        var expiry = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds();
        var token = protector.Protect($"0:{memberTbKey}:{expiry}");
        return Json(new { success = true, token });
    }

    public async Task<IActionResult> Detail(int id)
    {
        var member = await _memberService.GetMemberAsync(id);
        if (member == null) return NotFound();
        return View(member);
    }

    [HttpGet]
    public async Task<IActionResult> GetMemberJson(int id)
    {
        var member = await _memberService.GetMemberAsync(id);
        if (member == null) return NotFound();
        return Json(new
        {
            tbKey = member.TbKey,
            id = member.ID,
            email = member.Email,
            cName = member.CName,
            tel = member.Tel,
            addr = member.Addr,
            memberStatus = member.MemberStatus,
            userLanguage = member.UserLanguage,
            userUnit = member.UserUnit,
            timezoom = member.Timezoom,
            isSendEmail = member.IsSendEmail,
            isPush = member.IsPush,
            cDate = member.CDate.ToString("yyyy/MM/dd"),
            dynaCheckCode = member.DynaCheckCode,
            password = member.Password,
            obmTbKey = member.OBM_TbKey
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAjax(int tbKey, string? email, string? tel, string? addr, string? memberStatus)
    {
        var existing = await _memberService.GetMemberAsync(tbKey);
        if (existing == null) return Json(new { success = false, message = "找不到會員" });

        existing.Email = email ?? string.Empty;
        existing.Tel   = tel   ?? string.Empty;
        existing.Addr  = addr  ?? string.Empty;

        var result = await _memberService.UpdateMemberAsync(existing);
        if (!result.Success) return Json(new { success = false, message = result.Message });

        if (!string.IsNullOrEmpty(memberStatus))
            await _memberService.SetMemberStatusAsync(tbKey, memberStatus);

        return Json(new { success = true });
    }

    [HttpGet]
    public IActionResult Create() => View(new Core.Models.Member());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Core.Models.Member member, string password)
    {
        var result = await _memberService.RegisterMemberAsync(member, password);
        if (!result.Success) { ViewBag.Error = result.Message; return View(member); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Core.Models.Member member)
    {
        var result = await _memberService.UpdateMemberAsync(member);
        if (!result.Success) return BadRequest(result.Message);
        return RedirectToAction(nameof(Detail), new { id = member.TbKey });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, string status)
    {
        var result = await _memberService.SetMemberStatusAsync(id, status);
        if (!result.Success) return BadRequest(result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _memberService.DeleteMemberAsync(id);
        if (!result.Success) return BadRequest(result.Message);
        return RedirectToAction(nameof(Index));
    }
}
