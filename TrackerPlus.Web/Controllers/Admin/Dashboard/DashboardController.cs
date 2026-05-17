using Microsoft.AspNetCore.Mvc;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Services;

namespace TrackerPlus.Web.Controllers.Admin.Dashboard;

public class DashboardController : AdminBaseController
{
    private readonly ITrackerService _trackerService;
    private readonly IMemberService _memberService;
    private readonly IIMEIService _imeiService;

    public DashboardController(ITrackerService trackerService, IMemberService memberService, IIMEIService imeiService)
    {
        _trackerService = trackerService;
        _memberService = memberService;
        _imeiService = imeiService;
    }

    public async Task<IActionResult> Index()
    {
        var filter = new QueryFilter { PageSize = 1, PageIndex = 1 };
        ViewBag.TrackerCount = await _trackerService.GetTrackerCountAsync();
        ViewBag.MemberCount = await _memberService.GetMemberCountAsync(filter);
        ViewBag.IMEICount = await _imeiService.GetIMEICountAsync(filter);
        return View();
    }
}
