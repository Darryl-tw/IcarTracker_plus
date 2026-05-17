using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Services.Services;

public class DeviceSettingsService : IDeviceSettingsService
{
    private readonly IDeviceSettingsRepository _repo;
    private readonly ITrackerRepository _trackerRepo;
    private readonly ILogger<DeviceSettingsService> _logger;

    public DeviceSettingsService(
        IDeviceSettingsRepository repo,
        ITrackerRepository trackerRepo,
        ILogger<DeviceSettingsService> logger)
    {
        _repo = repo;
        _trackerRepo = trackerRepo;
        _logger = logger;
    }

    public async Task<DeviceSettingsDto?> GetSettingsAsync(int trackerTbKey, int memberTbKey)
    {
        var tracker = await _trackerRepo.GetByIdAsync(trackerTbKey);
        if (tracker == null || tracker.Member_TbKey != memberTbKey) return null;
        return await _repo.GetSettingsAsync(trackerTbKey, memberTbKey);
    }

    public async Task<DeviceDetailDto?> GetDetailAsync(int trackerTbKey, int memberTbKey, int timezoneMinutes)
    {
        var tracker = await _trackerRepo.GetByIdAsync(trackerTbKey);
        if (tracker == null || tracker.Member_TbKey != memberTbKey) return null;

        var detail = await _repo.GetDetailAsync(trackerTbKey, memberTbKey, timezoneMinutes);
        if (detail == null) return null;

        // 與即時地圖 marker 相同來源，避免詳情窗座標為 0
        if (Math.Abs(tracker.Lat) > 0.0001 || Math.Abs(tracker.Lng) > 0.0001)
        {
            detail.Lat = tracker.Lat;
            detail.Lng = tracker.Lng;
        }
        detail.SpeedKmh = tracker.Speed;
        detail.DirectionText = GpsHelper.DirectionTextFromDegrees(tracker.Direction);
        if (tracker.GPSNo > 0)
            detail.GpsUsed = tracker.GPSNo;
        if (tracker.Voltage > 0)
            detail.VoltagePercent = tracker.Voltage;

        return detail;
    }

    public async Task<OperationResult> SaveSettingsAsync(int trackerTbKey, int memberTbKey, DeviceSettingsSaveRequest request)
    {
        try
        {
            var tracker = await _trackerRepo.GetByIdAsync(trackerTbKey);
            if (tracker == null) return OperationResult.Fail("找不到裝置");
            if (tracker.Member_TbKey != memberTbKey) return OperationResult.Fail("無權限");

            var icon = (request.IconFile ?? "A").Trim().ToUpperInvariant();
            if (icon is not ("A" or "B" or "C" or "D" or "E" or "F" or "G" or "H"))
                icon = "A";

            var udPairs = (request.UdFields ?? [])
                .Select(f => (f.FieldTbKey, f.Value ?? ""))
                .ToList();

            if (!await _repo.SaveInfoAsync(trackerTbKey, request.Cname ?? "", icon, udPairs, request.LabelTbKeys ?? []))
                return OperationResult.Fail("儲存裝置資料失敗");

            if (!await _repo.SaveOptionAsync(trackerTbKey, request.GpsReportTime, request.ShockMode, request.ShockSensitive))
                return OperationResult.Fail("儲存裝置設定失敗");

            var sharePwd = (request.SharePassword ?? "0000").Trim();
            if (sharePwd.Length != 4) sharePwd = "0000";

            if (!await _repo.SaveAlertAsync(trackerTbKey, memberTbKey,
                    request.ApMinute, request.ApMsg ?? "", request.AsMinute, request.AsMsg ?? "",
                    request.ShareEnabled, sharePwd, request.AlertMode))
                return OperationResult.Fail("儲存警報設定失敗");

            return OperationResult.Ok("更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveSettings TbKey={TbKey}", trackerTbKey);
            return OperationResult.Fail(ex.Message);
        }
    }
}
