using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Web.Resources;

namespace TrackerPlus.Web.Controllers;

[Authorize]
[Route("Tracker/[action]/{id?}")]
public class TrackerController : Controller
{
    private readonly ITrackerService _trackerService;
    private readonly ILabelService _labelService;
    private readonly IStringLocalizer<SharedResources> _localizer;
    private readonly IGoogleApiKeyService _googleApiKeyService;

    public TrackerController(ITrackerService trackerService, ILabelService labelService,
        IStringLocalizer<SharedResources> localizer, IGoogleApiKeyService googleApiKeyService)
    {
        _trackerService = trackerService;
        _labelService = labelService;
        _localizer = localizer;
        _googleApiKeyService = googleApiKeyService;
    }

    private int GetMemberTbKey()
    {
        var val = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(val, out var key) ? key : 0;
    }

    [HttpGet]
    public IActionResult Index() => RedirectToAction("Live", "Map");

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var memberTbKey = GetMemberTbKey();
        var tracker = await _trackerService.GetTrackerAsync(id);
        if (tracker == null) return NotFound();
        if (tracker.Member_TbKey != memberTbKey) return Forbid();
        ViewBag.MemberLabels = await _labelService.GetMemberLabelsAsync(memberTbKey);
        ViewBag.AssignedLabelIds = (await _labelService.GetAssignedLabelTbKeysAsync(id)).ToHashSet();
        return View(tracker);
    }

    [HttpGet]
    public async Task<IActionResult> Map()
    {
        var memberTbKey = GetMemberTbKey();
        var trackers = await _trackerService.GetMemberTrackersAsync(memberTbKey);
        ViewBag.GoogleApiJs = _googleApiKeyService.GetMapsJavaScriptUrl(Request.Host.Host);
        return View(trackers);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateInfo(int tbKey, string cname, string memo, string label, string groupName)
    {
        var memberTbKey = GetMemberTbKey();
        var tracker = await _trackerService.GetTrackerAsync(tbKey);
        if (tracker == null) return NotFound();
        if (tracker.Member_TbKey != memberTbKey) return Forbid();

        var result = await _trackerService.UpdateTrackerInfoAsync(tbKey, cname, memo, label, groupName);
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Detail), new { id = tbKey });
        }
        TempData["Success"] = _localizer["Msg_BasicInfoUpdated"].Value;
        return RedirectToAction(nameof(Detail), new { id = tbKey });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOption(int tbKey, string sosNumber, string powerSavingMode)
    {
        var memberTbKey = GetMemberTbKey();
        var tracker = await _trackerService.GetTrackerAsync(tbKey);
        if (tracker == null) return NotFound();
        if (tracker.Member_TbKey != memberTbKey) return Forbid();

        var result = await _trackerService.UpdateTrackerOptionAsync(tbKey, sosNumber, powerSavingMode);
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Detail), new { id = tbKey });
        }
        TempData["Success"] = _localizer["Msg_AdvancedUpdated"].Value;
        return RedirectToAction(nameof(Detail), new { id = tbKey });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateLabels(int tbKey, int[]? labelTbKeys)
    {
        var memberTbKey = GetMemberTbKey();
        var tracker = await _trackerService.GetTrackerAsync(tbKey);
        if (tracker == null) return NotFound();
        if (tracker.Member_TbKey != memberTbKey) return Forbid();

        var result = await _trackerService.UpdateTrackerLabelsAsync(tbKey, memberTbKey, labelTbKeys ?? Array.Empty<int>());
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Detail), new { id = tbKey });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLabel(string labelName, int returnTrackerTbKey)
    {
        var memberTbKey = GetMemberTbKey();
        var result = await _labelService.CreateLabelAsync(memberTbKey, labelName);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Detail), new { id = returnTrackerTbKey });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLabel(int labelTbKey, int returnTrackerTbKey)
    {
        var memberTbKey = GetMemberTbKey();
        var result = await _labelService.DeleteLabelAsync(labelTbKey, memberTbKey);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Detail), new { id = returnTrackerTbKey });
    }
}
