using Microsoft.AspNetCore.Mvc;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Web.Controllers.Admin.Firmware;

public class FirmwareController : AdminBaseController
{
    private readonly IFirmwareService _firmwareService;

    public FirmwareController(IFirmwareService firmwareService)
    {
        _firmwareService = firmwareService;
    }

    public async Task<IActionResult> Index(string? keyword, int page = 1)
    {
        var filter = new QueryFilter { Keyword = keyword, PageIndex = page, PageSize = 100 };
        var result = await _firmwareService.GetFirmwaresPagedAsync(filter);
        ViewBag.Filter = filter;
        return View(result);
    }

    [HttpGet]
    public IActionResult Create() => View(new FirmwareVersion());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FirmwareVersion firmware)
    {
        var result = await _firmwareService.CreateFirmwareAsync(firmware);
        if (!result.Success) { ViewBag.Error = result.Message; return View(firmware); }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string fwVersion)
    {
        var fw = await _firmwareService.GetFirmwareAsync(fwVersion);
        if (fw == null) return NotFound();
        ViewBag.OriginalFwVersion = fwVersion;
        return View(fw);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string originalFwVersion, FirmwareVersion firmware)
    {
        var result = await _firmwareService.UpdateFirmwareAsync(firmware, originalFwVersion);
        if (!result.Success) { ViewBag.Error = result.Message; ViewBag.OriginalFwVersion = originalFwVersion; return View(firmware); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string fwVersion)
    {
        var result = await _firmwareService.DeleteFirmwareAsync(fwVersion);
        if (!result.Success) return BadRequest(result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BatchQueue(string targetFwVersion, string imeiList)
    {
        var list = imeiList.Split(new[] { ',', '\n', '\r', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var result = await _firmwareService.BatchQueueFirmwareUpdateAsync(targetFwVersion, list);
        if (!result.Success) return BadRequest(result.Message);
        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
