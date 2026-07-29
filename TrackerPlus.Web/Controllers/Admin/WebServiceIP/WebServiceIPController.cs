using Microsoft.AspNetCore.Mvc;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;
using TrackerPlus.Web.Helpers;

namespace TrackerPlus.Web.Controllers.Admin.WebServiceIP;

public class WebServiceIPController : AdminBaseController
{
    private readonly IWebServiceIPService _ipService;

    public WebServiceIPController(IWebServiceIPService ipService)
    {
        _ipService = ipService;
    }

    public async Task<IActionResult> Index(string? keyword, string? sortBy, bool sortDesc = false, int page = 1)
    {
        var filter = new QueryFilter { Keyword = keyword, PageIndex = page, PageSize = 100 };
        GridSortHelper.ApplySort(filter, sortBy, sortDesc, "tbKey", defaultDesc: true);
        var result = await _ipService.GetPagedAsync(filter);
        ViewBag.Filter = filter;
        return View(result);
    }

    [HttpGet]
    public IActionResult Create() => View(new Core.Models.WebServiceIP());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Core.Models.WebServiceIP ip)
    {
        var result = await _ipService.CreateAsync(ip);
        if (!result.Success) { ViewBag.Error = result.Message; return View(ip); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _ipService.DeleteAsync(id);
        if (!result.Success) return BadRequest(result.Message);
        return RedirectToAction(nameof(Index));
    }
}
