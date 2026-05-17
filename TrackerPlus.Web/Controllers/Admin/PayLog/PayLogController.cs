using Microsoft.AspNetCore.Mvc;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Web.Controllers.Admin.PayLog;

public class PayLogController : AdminBaseController
{
    private readonly IPayLogService _payLogService;
    private readonly IMemberService _memberService;

    public PayLogController(IPayLogService payLogService, IMemberService memberService)
    {
        _payLogService = payLogService;
        _memberService = memberService;
    }

    public async Task<IActionResult> Index(string? keyword, string? status, DateTime? startDate, DateTime? endDate, int page = 1)
    {
        var filter = new QueryFilter
        {
            Keyword = keyword,
            Status = status,
            StartDate = startDate,
            EndDate = endDate,
            PageIndex = page,
            PageSize = 100
        };
        var result = await _payLogService.GetPayLogsPagedAsync(filter);
        ViewBag.Filter = filter;
        return View(result);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var payLog = await _payLogService.GetPayLogAsync(id);
        if (payLog == null) return NotFound();
        return View(payLog);
    }

    [HttpGet]
    public IActionResult Create() => View(new Core.Models.PayLog { CDate = DateTime.Now });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Core.Models.PayLog payLog)
    {
        var result = await _payLogService.CreatePayLogAsync(payLog);
        if (!result.Success) { ViewBag.Error = result.Message; return View(payLog); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Core.Models.PayLog payLog)
    {
        var result = await _payLogService.UpdatePayLogAsync(payLog);
        if (!result.Success) return BadRequest(result.Message);
        return RedirectToAction(nameof(Detail), new { id = payLog.TbKey });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _payLogService.DeletePayLogAsync(id);
        if (!result.Success) return BadRequest(result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(int id, int newMemberTbKey)
    {
        var result = await _payLogService.MovePayLogAsync(id, newMemberTbKey);
        if (!result.Success) return BadRequest(result.Message);
        return RedirectToAction(nameof(Index));
    }
}
