using Microsoft.AspNetCore.Mvc;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Web.Controllers.Admin.IMEI;

public class IMEIController : AdminBaseController
{
    private readonly IIMEIService _imeiService;
    private readonly IMemberService _memberService;

    public IMEIController(IIMEIService imeiService, IMemberService memberService)
    {
        _imeiService = imeiService;
        _memberService = memberService;
    }

    public async Task<IActionResult> Index(string? keyword, string? status, int page = 1)
    {
        var filter = new QueryFilter { Keyword = keyword, Status = status, PageIndex = page, PageSize = 100 };
        var result = await _imeiService.GetIMEIsPagedAsync(filter);
        ViewBag.Filter = filter;
        return View(result);
    }

    [HttpGet]
    public IActionResult Create() => View(new IMEIDevice());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IMEIDevice device)
    {
        var result = await _imeiService.RegisterIMEIAsync(device);
        if (!result.Success) { ViewBag.Error = result.Message; return View(device); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _imeiService.DeleteIMEIAsync(id);
        if (!result.Success) return BadRequest(result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BatchDelete(string imeiList)
    {
        var list = imeiList.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var result = await _imeiService.BatchDeleteIMEIAsync(list);
        if (!result.Success) return BadRequest(result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(string imei, int newMemberTbKey)
    {
        var result = await _imeiService.MoveIMEIAsync(imei, newMemberTbKey, 0);
        if (!result.Success) return BadRequest(result.Message);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>僅解除會員綁定</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reset(string imei)
    {
        var result = await _imeiService.ResetIMEIAsync(imei);
        if (!result.Success) return BadRequest(result.Message);
        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>完整重置（刪 Tracker、歷史表、重建裝置）</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FullReset(string imei, bool deleteHistory = true, bool resetPayLog = false)
    {
        var options = new IMEIFullResetOptions
        {
            DeleteHistory = deleteHistory,
            ResetPayLog = resetPayLog
        };
        var result = await _imeiService.FullResetIMEIAsync(imei, options);
        if (!result.Success) return BadRequest(result.Message);
        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
