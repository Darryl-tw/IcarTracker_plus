using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Web.Controllers.Admin.Tracker;

public class TrackerController : AdminBaseController
{
    private readonly ITrackerService _trackerService;
    private readonly IMemberService _memberService;
    private readonly IPayLogService _payLogService;
    private readonly IDeviceSettingsService _deviceSettingsService;

    public TrackerController(ITrackerService trackerService, IMemberService memberService,
        IPayLogService payLogService, IDeviceSettingsService deviceSettingsService)
    {
        _trackerService = trackerService;
        _memberService = memberService;
        _payLogService = payLogService;
        _deviceSettingsService = deviceSettingsService;
    }

    public IActionResult Index() => View();

    [HttpGet]
    public async Task TrackerStream(string? obm, string? imei, string? account, string? keyword, string? status, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no");

        var filter = new QueryFilter
        {
            Obm = obm,
            Imei = imei,
            Account = account,
            Keyword = keyword,
            Status = status,
            PageIndex = page,
            PageSize = pageSize
        };
        var result = await _trackerService.GetTrackersPagedAsync(filter);

        await WriteEvent(new { type = "total", count = result.TotalCount, totalPages = result.TotalPages }, ct);

        foreach (var t in result.Items)
        {
            if (ct.IsCancellationRequested) break;
            await WriteEvent(new
            {
                type = "row",
                tbKey = t.TbKey,
                imei = t.IMEICODE,
                memberTbKey = t.Member_TbKey,
                obmName = t.OBMName,
                memberAccount = t.MemberAccount,
                cname = t.CName,
                bindCount = 0,
                createDate = t.CreateDate?.ToString("yyyy-MM-dd"),
                serviceEndDate = t.ServiceEndDate?.ToString("yyyy-MM-dd"),
                currentStatus = t.CurrentStatus,
                isSleep = t.IsSleep,
                firmwareVersion = t.FirmwareVersion,
                iccid = t.ICCID,
                apn = t.APN
            }, ct);
        }

        if (!ct.IsCancellationRequested)
            await WriteEvent(new { type = "done" }, ct);
    }

    [HttpGet]
    public async Task<IActionResult> GetPayLogs(string imei)
    {
        var logs = await _payLogService.GetPayLogsByIMEIAsync(imei);
        return Json(logs.Select(l => new
        {
            tbKey = l.TbKey,
            orderNo = l.OrderNo,
            amount = l.Amount,
            saleModel = l.SaleModel,
            sDate = l.SDate?.ToString("yyyy-MM-dd"),
            eDate = l.EDate?.ToString("yyyy-MM-dd"),
            cDate = l.CDate.ToString("yyyy-MM-dd"),
            status = l.Status,
            op = l.Operator,
            saleType = l.SaleMemo,
            memo = l.Memo,
            valueAdded = l.ValueAddedWeb
        }));
    }

    [HttpGet]
    public async Task<IActionResult> GetMemberTrackers(int memberTbKey, int trackerTbKey = 0)
    {
        IEnumerable<Core.Models.Tracker> trackers;
        Core.Models.Member? member = null;

        if (memberTbKey > 0)
        {
            trackers = await _trackerService.GetMemberTrackersAsync(memberTbKey);
            member = await _memberService.GetMemberAsync(memberTbKey);
        }
        else if (trackerTbKey > 0)
        {
            var single = await _trackerService.GetTrackerAsync(trackerTbKey);
            trackers = single != null ? [single] : [];
        }
        else
        {
            return Json(Array.Empty<object>());
        }

        var tzMinutes = member?.Timezoom ?? 480;
        var sign = tzMinutes >= 0 ? "+" : "-";
        var abs = Math.Abs(tzMinutes);
        var tzStr = $"UTC {sign} {abs / 60:00}:{abs % 60:00}";

        return Json(trackers.Select(t => new
        {
            tbKey = t.TbKey,
            imei = t.IMEICODE,
            cname = t.CName,
            trackerStatus = t.TrackerStatus,
            currentStatus = t.CurrentStatus,
            isSleep = t.IsSleep,
            lastReportTime = t.LastReportTime.HasValue
                ? t.LastReportTime.Value.AddMinutes(tzMinutes).ToString("yyyy-MM-dd HH:mm:ss") + " " + tzStr
                : (string?)null,
            serviceEndDate = t.ServiceEndDate?.ToString("yyyy-MM-dd"),
            effectiveDays = t.EffectiveDays,
            createDate = t.CreateDate?.ToString("yyyy-MM-dd")
        }));
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
        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAll(int memberTbKey)
    {
        var result = await _trackerService.DeleteAllTrackersByMemberAsync(memberTbKey);
        if (!result.Success) return BadRequest(result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetAdminDeviceSettings(int tbKey)
    {
        var tracker = await _trackerService.GetTrackerAsync(tbKey);
        if (tracker == null) return NotFound();
        var dto = await _deviceSettingsService.GetSettingsAsync(tbKey, tracker.Member_TbKey);
        if (dto == null) return NotFound();
        return Json(new
        {
            tbKey = dto.TbKey,
            imei = dto.Imei,
            cName = dto.CName,
            iconFile = dto.IconFile,
            udFields = dto.UdFields,
            labels = dto.Labels,
            sosNumber = tracker.SosNumber,
            powerSavingMode = tracker.PowerSavingMode ? "Y" : "N",
            gpsReportTime = dto.GpsReportTime,
            shockMode = dto.ShockMode,
            shockSensitive = dto.ShockSensitive,
            apMinute = dto.ApMinute,
            apMsg = dto.ApMsg,
            asMinute = dto.AsMinute,
            asMsg = dto.AsMsg,
            shareEnabled = dto.ShareEnabled,
            sharePassword = dto.SharePassword,
            alertMode = dto.AlertMode
        });
    }

    [HttpPost]
    public async Task<IActionResult> SaveAdminDeviceSettings([FromBody] AdminDeviceSettingsSaveRequest req)
    {
        if (req == null || req.TbKey <= 0) return BadRequest();
        var tracker = await _trackerService.GetTrackerAsync(req.TbKey);
        if (tracker == null) return NotFound();

        var saveReq = new DeviceSettingsSaveRequest
        {
            Cname = req.CName ?? "",
            IconFile = req.IconFile ?? "A",
            UdFields = (req.UdFields ?? []).Select(f => new DeviceUdFieldSave { FieldTbKey = f.FieldTbKey, Value = f.Value ?? "" }).ToList(),
            LabelTbKeys = req.LabelTbKeys ?? [],
            GpsReportTime = req.GpsReportTime,
            ShockMode = req.ShockMode,
            ShockSensitive = req.ShockSensitive,
            ApMinute = req.ApMinute,
            ApMsg = req.ApMsg ?? "",
            AsMinute = req.AsMinute,
            AsMsg = req.AsMsg ?? "",
            ShareEnabled = req.ShareEnabled,
            SharePassword = req.SharePassword ?? "0000",
            AlertMode = req.AlertMode
        };

        var result = await _deviceSettingsService.SaveSettingsAsync(req.TbKey, tracker.Member_TbKey, saveReq);
        if (!result.Success)
            return Json(new { success = false, message = result.Message });

        await _trackerService.UpdateTrackerOptionAsync(req.TbKey, req.SosNumber ?? "", req.PowerSavingMode ?? "N");
        return Json(new { success = true });
    }

    private async Task WriteEvent(object data, CancellationToken ct)
    {
        try
        {
            var json = JsonSerializer.Serialize(data);
            await Response.WriteAsync($"data: {json}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
        catch (OperationCanceledException) { }
    }
}

public class AdminDeviceSettingsSaveRequest
{
    public int TbKey { get; set; }
    public string? CName { get; set; }
    public string? IconFile { get; set; }
    public List<DeviceUdFieldSave>? UdFields { get; set; }
    public List<int>? LabelTbKeys { get; set; }
    public string? SosNumber { get; set; }
    public string? PowerSavingMode { get; set; }
    public int GpsReportTime { get; set; }
    public int ShockMode { get; set; }
    public int ShockSensitive { get; set; }
    public int ApMinute { get; set; }
    public string? ApMsg { get; set; }
    public int AsMinute { get; set; }
    public string? AsMsg { get; set; }
    public bool ShareEnabled { get; set; }
    public string? SharePassword { get; set; }
    public int AlertMode { get; set; }
}
