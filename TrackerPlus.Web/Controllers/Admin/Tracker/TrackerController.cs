using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
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
    private readonly IDealerRepository _dealerRepo;
    private readonly ISubAdminRepository _subAdminRepo;
    private readonly IDataProtectionProvider _dataProtection;

    public TrackerController(ITrackerService trackerService, IMemberService memberService,
        IPayLogService payLogService, IDeviceSettingsService deviceSettingsService,
        IHistoryService historyService, IFirmwareService firmwareService, IOBMService obmService,
        IDealerRepository dealerRepo, ISubAdminRepository subAdminRepo, IDataProtectionProvider dataProtection)
    {
        _trackerService = trackerService;
        _memberService = memberService;
        _payLogService = payLogService;
        _deviceSettingsService = deviceSettingsService;
        _historyService = historyService;
        _firmwareService = firmwareService;
        _obmService = obmService;
        _dealerRepo = dealerRepo;
        _subAdminRepo = subAdminRepo;
        _dataProtection = dataProtection;
    }

    public IActionResult Index() => View();

    // ── 產生前台預覽 Token（5 分鐘有效，讓管理員免登入查看前台地圖） ────────
    [HttpGet]
    public IActionResult GetPreviewToken(int trackerTbKey, int memberTbKey)
    {
        if (trackerTbKey <= 0)
            return Json(new { success = false, message = L["Admin_Error_NoDeviceSelected"].Value });
        var protector = _dataProtection.CreateProtector("AdminPreview");
        var expiry = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds();
        var token = protector.Protect($"{trackerTbKey}:{memberTbKey}:{expiry}");
        return Json(new { success = true, token });
    }

    [HttpGet]
    public async Task TrackerStream(string? obm, string? imei, string? account, string? keyword, string? status,
        string? sortBy = null, bool sortDesc = false, int page = 1, int pageSize = 20, CancellationToken ct = default)
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
            SortBy = string.IsNullOrWhiteSpace(sortBy) ? "imei" : sortBy.Trim(),
            SortDesc = sortDesc,
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
                bindCount = t.BindCount,
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
            return Json(new { success = false, message = L["Admin_Error_ImeiRequired"].Value });
        if (req.Imei.Trim().Length != 15)
            return Json(new { success = false, message = L["Admin_Error_Imei15Digits"].Value });

        var tracker = new Core.Models.Tracker
        {
            IMEICODE = req.Imei.Trim(),
            CName = req.CName?.Trim() ?? string.Empty,
            Member_TbKey = 0,
            TrackerStatus = "Y"
        };
        var result = await _trackerService.CreateTrackerAsync(tracker);
        return Json(new
        {
            success = result.Success,
            message = result.Success ? L["Admin_Tracker_Msg_CreateOk"].Value : result.Message
        });
    }

    // ── 清除歷史資料 ─────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> ClearHistory([FromBody] ClearHistoryRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Imei))
            return Json(new { success = false, message = L["Admin_Error_SelectDevice"].Value });

        OperationResult result;
        if (req.DeleteAll)
        {
            result = await _historyService.DeleteAllHistoryAsync(req.Imei.Trim());
        }
        else
        {
            if (!DateTime.TryParse(req.StartDate, out var localStart) ||
                !DateTime.TryParse(req.EndDate, out var localEnd))
                return Json(new { success = false, message = L["Admin_Error_InvalidDateFormat"].Value });
            localEnd = localEnd.Date.AddDays(1).AddSeconds(-1);
            result = await _historyService.DeleteHistoryAsync(req.Imei.Trim(), localStart, localEnd, 480);
        }
        var msg = result.Success
            ? (req.DeleteAll ? L["Admin_Tracker_Msg_ClearAllHistoryOk"].Value : L["Admin_Tracker_Msg_ClearHistoryOk"].Value)
            : result.Message;
        return Json(new { success = result.Success, message = msg });
    }

    // ── 批次轉移 OBM ─────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> BatchTransfer([FromBody] BatchTransferRequest req)
    {
        if (req == null || req.Imeis == null || !req.Imeis.Any() || req.OBMTbKey <= 0)
            return Json(new { success = false, message = L["Admin_Error_InvalidParams"].Value });

        if (string.IsNullOrWhiteSpace(req.SubAdminId) || string.IsNullOrWhiteSpace(req.SubAdminPassword))
            return Json(new { success = false, message = L["Admin_Tracker_SubAdminRequired"].Value });

        var saleModel = req.DefaultPay ? req.SaleModel : 0;
        var subAdminKey = await _subAdminRepo.ValidateMoveDeviceAsync(
            req.SubAdminId, req.SubAdminPassword, req.DefaultPay, saleModel);
        if (subAdminKey is null or <= 0)
            return Json(new { success = false, message = L["Admin_Tracker_SubAdminInvalid"].Value });

        if (req.DefaultPay && string.Equals(req.EndDateStatus, "A", StringComparison.OrdinalIgnoreCase)
            && (req.EDate == null))
            return Json(new { success = false, message = L["Admin_PayLog_EdateBeforeSdate"].Value });

        var dealer = await _dealerRepo.GetByIdAsync(req.OBMTbKey);
        var moveReq = new BatchMoveDevicesRequest
        {
            Imeis = req.Imeis.Select(x => x.Trim()).Where(x => x.Length > 0).ToList(),
            OBMTbKey = req.OBMTbKey,
            ResetOnlineTime = req.ResetOnlineTime,
            DefaultPay = req.DefaultPay,
            OrderNo = req.OrderNo?.Trim() ?? string.Empty,
            SaleModel = req.SaleModel,
            SDate = req.SDate,
            EDate = req.EDate,
            EndDateStatus = req.EndDateStatus?.Trim() ?? "1",
            FMonth = req.FMonth,
            Amount = req.Amount,
            ValueAddedWeb = req.ValueAddedWeb,
            SaleMemo = req.SaleMemo?.Trim() ?? string.Empty,
            IsSimBundled = req.IsSimBundled,
            IconFile = req.IconFile?.Trim() ?? string.Empty,
            SubAdminUserTbKey = subAdminKey.Value,
            TargetDealerLabel = dealer != null
                ? $"{dealer.UserName.Trim()}({dealer.UserID.Trim()})"
                : req.OBMTbKey.ToString()
        };

        var result = await _trackerService.BatchMoveDevicesAsync(moveReq);
        var msg = result.Success
            ? (result.Message ?? string.Format(L["Admin_Tracker_Msg_BatchTransferOk"].Value, result.Data ?? 0))
            : (result.Message == "無 IMEI" ? L["Admin_Tracker_Msg_NoImei"].Value : result.Message);
        return Json(new { success = result.Success, message = msg });
    }

    // ── 韌體昇級 ─────────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> BatchFirmwareUpgrade([FromBody] BatchFirmwareRequest req)
    {
        if (req == null || req.Imeis == null || !req.Imeis.Any() || string.IsNullOrWhiteSpace(req.TargetVersion))
            return Json(new { success = false, message = L["Admin_Error_InvalidParams"].Value });
        var result = await _firmwareService.BatchQueueFirmwareUpdateAsync(req.TargetVersion, req.Imeis);
        string msg;
        if (result.Success)
        {
            var count = result.Data is int c ? c : req.Imeis.Count();
            msg = string.Format(L["Admin_Tracker_Msg_FwUpgradeOk"].Value, count, req.TargetVersion);
        }
        else if (result.Message == "找不到韌體版本")
            msg = L["Admin_Tracker_Msg_FwVersionNotFound"].Value;
        else if (result.Message == "未提供 IMEI")
            msg = L["Admin_Tracker_Msg_NoImei"].Value;
        else if (result.Message == "排程失敗")
            msg = L["Admin_Tracker_Msg_FwQueueFail"].Value;
        else
            msg = result.Message;
        return Json(new { success = result.Success, message = msg });
    }

    // ── 整批刪除裝置（依 IMEI，需操作者 DelDevice 權限） ───────────────────────
    [HttpPost]
    public async Task<IActionResult> BatchDeleteDevices([FromBody] BatchDeleteRequest req)
    {
        if (req?.Imeis == null || !req.Imeis.Any())
            return Json(new { success = false, message = L["Admin_Tracker_EnterImei"].Value });

        if (string.IsNullOrWhiteSpace(req.SubAdminId) || string.IsNullOrWhiteSpace(req.SubAdminPassword))
            return Json(new { success = false, message = L["Admin_Tracker_SubAdminRequired"].Value });

        var subAdminKey = await _subAdminRepo.ValidateDeleteDeviceAsync(req.SubAdminId, req.SubAdminPassword);
        if (subAdminKey is null or <= 0)
            return Json(new { success = false, message = L["Admin_Tracker_SubAdminInvalid"].Value });

        var result = await _trackerService.BatchDeleteDevicesByImeiAsync(req.Imeis, subAdminKey.Value);
        if (!result.Success)
        {
            var failMsg = FormatBatchDeleteErrors(result.Message ?? string.Empty);
            return Json(new { success = false, message = failMsg });
        }

        var parts = (result.Message ?? "0").Split('|', 2);
        var deleted = int.TryParse(parts[0], out var n) ? n : 0;
        var message = string.Format(L["Admin_Error_BatchDeleted"].Value, deleted);
        if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
            message += "\n" + FormatBatchDeleteErrors(parts[1]);
        return Json(new { success = true, message });
    }

    // ── 單筆刪除裝置（對齊舊 IMEI_Del） ───────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> DeleteDevice([FromBody] DeleteDeviceRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Imei))
            return Json(new { success = false, message = L["Admin_Tracker_EnterImei"].Value });

        if (string.IsNullOrWhiteSpace(req.SubAdminId) || string.IsNullOrWhiteSpace(req.SubAdminPassword))
            return Json(new { success = false, message = L["Admin_Tracker_SubAdminRequired"].Value });

        var subAdminKey = await _subAdminRepo.ValidateDeleteDeviceAsync(req.SubAdminId, req.SubAdminPassword);
        if (subAdminKey is null or <= 0)
            return Json(new { success = false, message = L["Admin_Tracker_SubAdminInvalid"].Value });

        var result = await _trackerService.DeleteDeviceByImeiAsync(req.Imei.Trim(), subAdminKey.Value);
        if (!result.Success)
        {
            var msg = result.Message switch
            {
                "NOT_FOUND" => L["Admin_Tracker_Msg_DeviceNotFound"].Value,
                "INVALID_IMEI" => L["Admin_Tracker_EnterImei"].Value,
                _ => result.Message
            };
            return Json(new { success = false, message = msg });
        }
        return Json(new { success = true, message = L["Admin_Tracker_DeleteOk"].Value });
    }

    private string FormatBatchDeleteErrors(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw;
        if (raw == "NO_IMEI") return L["Admin_Tracker_EnterImei"].Value;
        var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return string.Join("\n", lines.Select(line =>
        {
            var idx = line.IndexOf(':');
            if (idx <= 0) return line;
            var imei = line[..idx];
            var code = line[(idx + 1)..];
            var reason = code switch
            {
                "NOT_FOUND" => L["Admin_Tracker_Msg_DeviceNotFound"].Value,
                "INVALID_IMEI" => L["Admin_Tracker_EnterImei"].Value,
                _ => code
            };
            return $"{imei}:{reason}";
        }));
    }

    // ── 整批刪除（刪除某會員所有裝置） ──────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> DeleteAllByMember([FromBody] DeleteAllByMemberRequest req)
    {
        if (req == null || req.MemberTbKey <= 0)
            return Json(new { success = false, message = L["Admin_Error_SelectMemberBoundDevice"].Value });
        var result = await _trackerService.DeleteAllTrackersByMemberAsync(req.MemberTbKey);
        return Json(new { success = result.Success, message = result.Message });
    }

    // ── 解除裝置綁定 ─────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> UnbindDevice([FromBody] SingleTbKeyRequest req)
    {
        if (req == null || req.TbKey <= 0)
            return Json(new { success = false, message = L["Admin_Error_NoDeviceSelected"].Value });
        var result = await _trackerService.UnbindDeviceAsync(req.TbKey);
        return Json(new { success = result.Success, message = result.Message });
    }

    // ── 解除會員所有裝置綁定 ─────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> UnbindAllByMember([FromBody] DeleteAllByMemberRequest req)
    {
        if (req == null || req.MemberTbKey <= 0)
            return Json(new { success = false, message = L["Admin_Error_SelectMemberBoundDevice"].Value });
        var result = await _trackerService.UnbindAllByMemberAsync(req.MemberTbKey);
        return Json(new { success = result.Success, message = result.Message });
    }

    // ── 恢復出廠 ─────────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> FactoryReset([FromBody] SingleTbKeyRequest req)
    {
        if (req == null || req.TbKey <= 0)
            return Json(new { success = false, message = L["Admin_Error_NoDeviceSelected"].Value });
        var result = await _trackerService.FactoryResetAsync(req.TbKey);
        var msg = result.Success
            ? L["Admin_Tracker_Msg_FactoryResetOk"].Value
            : (result.Message == "找不到裝置" ? L["Admin_Tracker_Msg_DeviceNotFound"].Value : result.Message);
        return Json(new { success = result.Success, message = msg });
    }

    // ── 轉移歷史軌跡 ─────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> MoveHistory([FromBody] MoveHistoryRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.SourceImei) || string.IsNullOrWhiteSpace(req.DestImei))
            return Json(new { success = false, message = L["Admin_Error_ImeiSourceTargetRequired"].Value });
        if (req.SourceImei.Trim() == req.DestImei.Trim())
            return Json(new { success = false, message = L["Admin_Error_ImeiSourceTargetSame"].Value });
        var result = await _historyService.QueueMoveHistoryAsync(req.SourceImei.Trim(), req.DestImei.Trim());
        var msg = result.Success
            ? L["Admin_Tracker_Msg_MoveHistoryOk"].Value
            : (result.Message == "加入佇列失敗" ? L["Admin_Tracker_Msg_MoveHistoryFail"].Value : result.Message);
        return Json(new { success = result.Success, message = msg });
    }

    [HttpGet]
    public async Task<IActionResult> GetDealerList()
    {
        var list = await _dealerRepo.GetAllAsync(null, null);
        return Json(list
            .OrderBy(o => o.UserName)
            .Select(o => new
            {
                tbKey = o.TbKey,
                name = $"{o.UserName.Trim()}({o.UserID.Trim()})"
            }));
    }

    // ── 取得 OBM 列表（韌體等仍可能使用） ─────────────────────────────────────
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
public class BatchTransferRequest
{
    public List<string>? Imeis { get; set; }
    public int OBMTbKey { get; set; }
    public bool ResetOnlineTime { get; set; }
    public bool DefaultPay { get; set; }
    public string? OrderNo { get; set; }
    public int SaleModel { get; set; }
    public DateTime? SDate { get; set; }
    public DateTime? EDate { get; set; }
    public string? EndDateStatus { get; set; }
    public int FMonth { get; set; } = 1;
    public decimal Amount { get; set; }
    public int ValueAddedWeb { get; set; }
    public string? SaleMemo { get; set; }
    public bool IsSimBundled { get; set; } = true;
    public string? IconFile { get; set; }
    public string? SubAdminId { get; set; }
    public string? SubAdminPassword { get; set; }
}
public class BatchFirmwareRequest { public List<string>? Imeis { get; set; } public string? TargetVersion { get; set; } }
public class BatchDeleteRequest
{
    public List<string>? Imeis { get; set; }
    public string? SubAdminId { get; set; }
    public string? SubAdminPassword { get; set; }
}
public class DeleteDeviceRequest
{
    public string? Imei { get; set; }
    public string? SubAdminId { get; set; }
    public string? SubAdminPassword { get; set; }
}
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
