using Microsoft.AspNetCore.DataProtection;
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
    private readonly IHistoryService _historyService;
    private readonly IFirmwareService _firmwareService;
    private readonly IOBMService _obmService;
    private readonly IDataProtectionProvider _dataProtection;

    public TrackerController(ITrackerService trackerService, IMemberService memberService,
        IPayLogService payLogService, IDeviceSettingsService deviceSettingsService,
        IHistoryService historyService, IFirmwareService firmwareService, IOBMService obmService,
        IDataProtectionProvider dataProtection)
    {
        _trackerService = trackerService;
        _memberService = memberService;
        _payLogService = payLogService;
        _deviceSettingsService = deviceSettingsService;
        _historyService = historyService;
        _firmwareService = firmwareService;
        _obmService = obmService;
        _dataProtection = dataProtection;
    }

    public IActionResult Index() => View();

    // ── 產生前台預覽 Token（5 分鐘有效，讓管理員免登入查看前台地圖） ────────
    [HttpGet]
    public IActionResult GetPreviewToken(int trackerTbKey, int memberTbKey)
    {
        if (trackerTbKey <= 0)
            return Json(new { success = false, message = "未選取裝置" });
        var protector = _dataProtection.CreateProtector("AdminPreview");
        var expiry = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds();
        var token = protector.Protect($"{trackerTbKey}:{memberTbKey}:{expiry}");
        return Json(new { success = true, token });
    }

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

    // ── 新增裝置 ─────────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> CreateTracker([FromBody] CreateTrackerRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Imei))
            return Json(new { success = false, message = "IMEI 不可空白" });
        if (req.Imei.Trim().Length != 15)
            return Json(new { success = false, message = "IMEI 需為 15 碼" });

        var tracker = new Core.Models.Tracker
        {
            IMEICODE = req.Imei.Trim(),
            CName = req.CName?.Trim() ?? string.Empty,
            Member_TbKey = 0,
            TrackerStatus = "Y"
        };
        var result = await _trackerService.CreateTrackerAsync(tracker);
        return Json(new { success = result.Success, message = result.Message });
    }

    // ── 清除歷史資料 ─────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> ClearHistory([FromBody] ClearHistoryRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Imei))
            return Json(new { success = false, message = "請選擇裝置" });

        OperationResult result;
        if (req.DeleteAll)
        {
            result = await _historyService.DeleteAllHistoryAsync(req.Imei.Trim());
        }
        else
        {
            if (!DateTime.TryParse(req.StartDate, out var localStart) ||
                !DateTime.TryParse(req.EndDate, out var localEnd))
                return Json(new { success = false, message = "日期格式錯誤" });
            localEnd = localEnd.Date.AddDays(1).AddSeconds(-1);
            result = await _historyService.DeleteHistoryAsync(req.Imei.Trim(), localStart, localEnd, 480);
        }
        return Json(new { success = result.Success, message = result.Message });
    }

    // ── 批次轉移 OBM ─────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> BatchTransfer([FromBody] BatchTransferRequest req)
    {
        if (req == null || req.Imeis == null || !req.Imeis.Any() || req.OBMTbKey <= 0)
            return Json(new { success = false, message = "參數不正確" });
        var result = await _trackerService.BatchTransferToOBMAsync(req.Imeis, req.OBMTbKey);
        return Json(new { success = result.Success, message = result.Message });
    }

    // ── 韌體昇級 ─────────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> BatchFirmwareUpgrade([FromBody] BatchFirmwareRequest req)
    {
        if (req == null || req.Imeis == null || !req.Imeis.Any() || string.IsNullOrWhiteSpace(req.TargetVersion))
            return Json(new { success = false, message = "參數不正確" });
        var result = await _firmwareService.BatchQueueFirmwareUpdateAsync(req.TargetVersion, req.Imeis);
        return Json(new { success = result.Success, message = result.Message });
    }

    // ── 整批刪除裝置（刪除選取的 tbKey 列表） ─────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> BatchDeleteDevices([FromBody] BatchDeleteRequest req)
    {
        if (req == null || req.TbKeys == null || !req.TbKeys.Any())
            return Json(new { success = false, message = "未選取裝置" });
        var deleted = await _trackerService.BatchDeleteByKeysAsync(req.TbKeys);
        return Json(new { success = true, message = $"已刪除 {deleted} 台裝置" });
    }

    // ── 整批刪除（刪除某會員所有裝置） ──────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> DeleteAllByMember([FromBody] DeleteAllByMemberRequest req)
    {
        if (req == null || req.MemberTbKey <= 0)
            return Json(new { success = false, message = "請先選擇有綁定會員的裝置" });
        var result = await _trackerService.DeleteAllTrackersByMemberAsync(req.MemberTbKey);
        return Json(new { success = result.Success, message = result.Message });
    }

    // ── 解除裝置綁定 ─────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> UnbindDevice([FromBody] SingleTbKeyRequest req)
    {
        if (req == null || req.TbKey <= 0)
            return Json(new { success = false, message = "未選取裝置" });
        var result = await _trackerService.UnbindDeviceAsync(req.TbKey);
        return Json(new { success = result.Success, message = result.Message });
    }

    // ── 解除會員所有裝置綁定 ─────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> UnbindAllByMember([FromBody] DeleteAllByMemberRequest req)
    {
        if (req == null || req.MemberTbKey <= 0)
            return Json(new { success = false, message = "請先選擇有綁定會員的裝置" });
        var result = await _trackerService.UnbindAllByMemberAsync(req.MemberTbKey);
        return Json(new { success = result.Success, message = result.Message });
    }

    // ── 恢復出廠 ─────────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> FactoryReset([FromBody] SingleTbKeyRequest req)
    {
        if (req == null || req.TbKey <= 0)
            return Json(new { success = false, message = "未選取裝置" });
        var result = await _trackerService.FactoryResetAsync(req.TbKey);
        return Json(new { success = result.Success, message = result.Message });
    }

    // ── 轉移歷史軌跡 ─────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> MoveHistory([FromBody] MoveHistoryRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.SourceImei) || string.IsNullOrWhiteSpace(req.DestImei))
            return Json(new { success = false, message = "來源與目標 IMEI 不可空白" });
        if (req.SourceImei.Trim() == req.DestImei.Trim())
            return Json(new { success = false, message = "來源與目標不可相同" });
        var result = await _historyService.QueueMoveHistoryAsync(req.SourceImei.Trim(), req.DestImei.Trim());
        return Json(new { success = result.Success, message = result.Message });
    }

    // ── 取得 OBM 列表 ─────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetOBMList()
    {
        var list = await _obmService.GetAllOBMsAsync();
        return Json(list.OrderBy(o => o.CName).Select(o => new { tbKey = o.TbKey, name = o.CName }));
    }

    // ── 取得韌體版本列表 ──────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetFirmwareVersions()
    {
        var list = await _firmwareService.GetAllFirmwaresAsync();
        return Json(list.OrderByDescending(f => f.FWVERSION).Select(f => new { version = f.FWVERSION, memo = f.NewFwVersion }));
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

public class CreateTrackerRequest { public string? Imei { get; set; } public string? CName { get; set; } }
public class ClearHistoryRequest { public string? Imei { get; set; } public bool DeleteAll { get; set; } public string? StartDate { get; set; } public string? EndDate { get; set; } }
public class BatchTransferRequest { public List<string>? Imeis { get; set; } public int OBMTbKey { get; set; } }
public class BatchFirmwareRequest { public List<string>? Imeis { get; set; } public string? TargetVersion { get; set; } }
public class BatchDeleteRequest { public List<int>? TbKeys { get; set; } }
public class DeleteAllByMemberRequest { public int MemberTbKey { get; set; } }
public class SingleTbKeyRequest { public int TbKey { get; set; } }
public class MoveHistoryRequest { public string? SourceImei { get; set; } public string? DestImei { get; set; } }

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
