using Microsoft.AspNetCore.Mvc;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Web.Controllers.Admin.Tracker;

public class TrackerController : AdminBaseController
{
    private readonly ITrackerService _trackerService;
    private readonly IMemberService _memberService;

    public TrackerController(ITrackerService trackerService, IMemberService memberService)
    {
        _trackerService = trackerService;
        _memberService = memberService;
    }

    public async Task<IActionResult> Index(string? keyword, string? status, int page = 1)
    {
        var filter = new QueryFilter { Keyword = keyword, Status = status, PageIndex = page, PageSize = 100 };
        var result = await _trackerService.GetTrackersPagedAsync(filter);
        ViewBag.Filter = filter;
        return View(result);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var tracker = await _trackerService.GetTrackerAsync(id);
        if (tracker == null) return NotFound();
        return View(tracker);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateInfo(int tbKey, string cname, string memo, string label, string groupName)
    {
        var result = await _trackerService.UpdateTrackerInfoAsync(tbKey, cname, memo, label, groupName);
        if (!result.Success) return BadRequest(result.Message);
        return RedirectToAction(nameof(Detail), new { id = tbKey });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOption(int tbKey, string sosNumber, string powerSavingMode)
    {
        var result = await _trackerService.UpdateTrackerOptionAsync(tbKey, sosNumber, powerSavingMode);
        if (!result.Success) return BadRequest(result.Message);
        return RedirectToAction(nameof(Detail), new { id = tbKey });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _trackerService.DeleteTrackerAsync(id);
        if (!result.Success) return BadRequest(result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAll(int memberTbKey)
    {
        var result = await _trackerService.DeleteAllTrackersByMemberAsync(memberTbKey);
        if (!result.Success) return BadRequest(result.Message);
        return RedirectToAction(nameof(Index));
    }
}
