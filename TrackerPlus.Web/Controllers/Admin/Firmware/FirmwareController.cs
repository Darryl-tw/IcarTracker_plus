using Microsoft.AspNetCore.Mvc;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;
using TrackerPlus.Web.Helpers;

namespace TrackerPlus.Web.Controllers.Admin.Firmware;

public class FirmwareController : AdminBaseController
{
    private readonly IFirmwareService _firmwareService;

    public FirmwareController(IFirmwareService firmwareService)
    {
        _firmwareService = firmwareService;
    }

    public async Task<IActionResult> Index(string? keyword, string? sortBy, bool sortDesc = false, int page = 1, int pageSize = 10)
    {
        pageSize = pageSize switch { 20 => 20, 50 => 50, 100 => 100, _ => 10 };
        if (page < 1) page = 1;

        var filter = new QueryFilter { Keyword = keyword, PageIndex = page, PageSize = pageSize };
        GridSortHelper.ApplySort(filter, sortBy, sortDesc, "cdate", defaultDesc: true);
        var result = await _firmwareService.GetFirmwaresPagedAsync(filter);
        if (result.TotalPages > 0 && page > result.TotalPages)
        {
            filter.PageIndex = result.TotalPages;
            result = await _firmwareService.GetFirmwaresPagedAsync(filter);
        }

        ViewBag.Filter = filter;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetJson(string fwVersion)
    {
        var fw = await _firmwareService.GetFirmwareAsync(fwVersion);
        if (fw == null) return NotFound(new { success = false, message = L["Admin_Error_InvalidParams"].Value });
        return Json(new
        {
            success = true,
            fwVersion = fw.FWVERSION,
            ftpServer = fw.FtpServer,
            ftpUsername = fw.FtpUsername,
            ftpPassword = fw.FtpPassword,
            ftpDir = fw.FtpDir,
            fileName = fw.FileName,
            fileSize = fw.FileSize,
            newFwVersion = fw.NewFwVersion
        });
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAjax([FromForm] FirmwareVersion firmware)
    {
        var result = await _firmwareService.CreateFirmwareAsync(firmware);
        return Json(new { success = result.Success, message = result.Message });
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
    public async Task<IActionResult> UpdateAjax(string originalFwVersion, [FromForm] FirmwareVersion firmware)
    {
        var result = await _firmwareService.UpdateFirmwareAsync(firmware, originalFwVersion);
        return Json(new { success = result.Success, message = result.Message });
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
    public async Task<IActionResult> DeleteAjax(string fwVersion)
    {
        var result = await _firmwareService.DeleteFirmwareAsync(fwVersion);
        return Json(new { success = result.Success, message = result.Message });
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BatchQueueAjax(string targetFwVersion, string imeiList)
    {
        var list = (imeiList ?? string.Empty)
            .Split(new[] { ',', '\n', '\r', ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var result = await _firmwareService.BatchQueueFirmwareUpdateAsync(targetFwVersion, list);
        return Json(new { success = result.Success, message = result.Message });
    }
}
