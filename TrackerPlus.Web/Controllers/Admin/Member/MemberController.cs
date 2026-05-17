using Microsoft.AspNetCore.Mvc;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Web.Controllers.Admin.Member;

public class MemberController : AdminBaseController
{
    private readonly IMemberService _memberService;
    private readonly IAuthService _authService;

    public MemberController(IMemberService memberService, IAuthService authService)
    {
        _memberService = memberService;
        _authService = authService;
    }

    public async Task<IActionResult> Index(string? keyword, string? status, int page = 1)
    {
        var filter = new QueryFilter { Keyword = keyword, Status = status, PageIndex = page, PageSize = 100 };
        var result = await _memberService.GetMembersAsync(filter);
        ViewBag.Filter = filter;
        return View(result);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var member = await _memberService.GetMemberAsync(id);
        if (member == null) return NotFound();
        return View(member);
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
