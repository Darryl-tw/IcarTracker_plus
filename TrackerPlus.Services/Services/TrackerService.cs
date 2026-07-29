using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Services.Services;

public class TrackerService : ITrackerService
{
    private readonly ITrackerRepository _trackerRepo;
    private readonly ILabelRepository _labelRepo;
    private readonly ILogger<TrackerService> _logger;

    public TrackerService(ITrackerRepository trackerRepo, ILabelRepository labelRepo, ILogger<TrackerService> logger)
    {
        _trackerRepo = trackerRepo;
        _labelRepo = labelRepo;
        _logger = logger;
    }

    public async Task<Tracker?> GetTrackerAsync(int tbKey)
    {
        var tracker = await _trackerRepo.GetByIdAsync(tbKey);
        if (tracker == null) return null;
        tracker.Label = await _labelRepo.GetLabelNamesDisplayAsync(tbKey, tracker.Member_TbKey);
        return tracker;
    }

    public async Task<Tracker?> GetTrackerByIMEIAsync(string imei)
        => await _trackerRepo.GetByIMEIAsync(imei);

    public async Task<IEnumerable<Tracker>> GetMemberTrackersAsync(int memberTbKey)
        => await _trackerRepo.GetByMemberAsync(memberTbKey);

    public async Task<PagedResult<Tracker>> GetTrackersPagedAsync(QueryFilter filter, int? memberTbKey = null)
        => await _trackerRepo.GetPagedAsync(filter, memberTbKey);

    public async Task<OperationResult> CreateTrackerAsync(Tracker tracker)
    {
        try
        {
            var newKey = await _trackerRepo.CreateAsync(tracker);
            return OperationResult.Ok("新增成功", newKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "新增追蹤器失敗 IMEI={IMEI}", tracker.IMEICODE);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> InsertDevicesAsync(IEnumerable<string> imeis, int subAdminUserTbKey)
    {
        try
        {
            return await _trackerRepo.InsertDevicesAsync(imeis, subAdminUserTbKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "後台新增裝置失敗 SubAdmin={Sub}", subAdminUserTbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> UpdateTrackerAsync(Tracker tracker)
    {
        try
        {
            var ok = await _trackerRepo.UpdateAsync(tracker);
            return ok ? OperationResult.Ok("更新成功") : OperationResult.Fail("找不到追蹤器");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新追蹤器失敗 TbKey={TbKey}", tracker.TbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteTrackerAsync(int tbKey)
    {
        try
        {
            var ok = await _trackerRepo.DeleteAsync(tbKey);
            return ok ? OperationResult.Ok("刪除成功") : OperationResult.Fail("找不到追蹤器");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除追蹤器失敗 TbKey={TbKey}", tbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteAllTrackersByMemberAsync(int memberTbKey)
    {
        try
        {
            var trackers = await _trackerRepo.GetByMemberAsync(memberTbKey);
            foreach (var t in trackers)
                await _trackerRepo.DeleteAsync(t.TbKey);
            return OperationResult.Ok("全部刪除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除會員全部追蹤器失敗 MemberTbKey={Key}", memberTbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> UpdateTrackerInfoAsync(int tbKey, string cname, string memo, string label, string groupName)
    {
        try
        {
            var tracker = await _trackerRepo.GetByIdAsync(tbKey);
            if (tracker == null) return OperationResult.Fail("找不到追蹤器");
            tracker.CName = cname;
            tracker.Memo = memo;
            var ok = await _trackerRepo.UpdateAsync(tracker);
            return ok ? OperationResult.Ok("更新成功") : OperationResult.Fail("更新失敗");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新追蹤器資訊失敗 TbKey={TbKey}", tbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> SaveDeviceSettingsAsync(int tbKey, int memberTbKey, string cname, string memo, string iconFile, IEnumerable<int> labelTbKeys)
    {
        try
        {
            var tracker = await _trackerRepo.GetByIdAsync(tbKey);
            if (tracker == null) return OperationResult.Fail("找不到追蹤器");
            if (tracker.Member_TbKey != memberTbKey) return OperationResult.Fail("無權限");

            var icon = (iconFile ?? "A").Trim().ToUpperInvariant();
            if (icon is not ("A" or "B" or "C" or "D" or "E" or "F" or "G" or "H"))
                icon = "A";

            var ok = await _trackerRepo.UpdateLiveSettingsAsync(tbKey, cname, memo, icon);
            if (!ok) return OperationResult.Fail("更新失敗");

            await _labelRepo.SetTrackerLabelsAsync(tbKey, labelTbKeys);
            return OperationResult.Ok("更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "儲存裝置設定失敗 TbKey={TbKey}", tbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> UpdateTrackerLabelsAsync(int tbKey, int memberTbKey, IEnumerable<int> labelTbKeys)
    {
        try
        {
            var tracker = await _trackerRepo.GetByIdAsync(tbKey);
            if (tracker == null) return OperationResult.Fail("找不到追蹤器");
            if (tracker.Member_TbKey != memberTbKey) return OperationResult.Fail("無權限");
            await _labelRepo.SetTrackerLabelsAsync(tbKey, labelTbKeys);
            return OperationResult.Ok("標籤已更新");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新標籤失敗 TbKey={TbKey}", tbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> UpdateTrackerOptionAsync(int tbKey, string sosNumber, string powerSavingMode)
    {
        try
        {
            var tracker = await _trackerRepo.GetByIdAsync(tbKey);
            if (tracker == null) return OperationResult.Fail("找不到追蹤器");
            tracker.SosNumber = sosNumber;
            tracker.PowerSavingMode = powerSavingMode == "Y";
            var ok = await _trackerRepo.UpdateAsync(tracker);
            return ok ? OperationResult.Ok("更新成功") : OperationResult.Fail("更新失敗");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新追蹤器選項失敗 TbKey={TbKey}", tbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<IEnumerable<Tracker>> GetLiveLocationsAsync(int memberTbKey)
        => await _trackerRepo.GetByMemberAsync(memberTbKey);

    public async Task<int> GetTrackerCountAsync(int? memberTbKey = null)
        => await _trackerRepo.GetCountAsync(memberTbKey);

    public async Task<OperationResult> BatchMoveDevicesAsync(BatchMoveDevicesRequest request)
    {
        try { return await _trackerRepo.BatchMoveDevicesAsync(request); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批次轉移經銷商失敗 OBM={OBM}", request.OBMTbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> FactoryResetAsync(int tbKey)
    {
        try { return await _trackerRepo.FactoryResetAsync(tbKey); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢復出廠失敗 TbKey={TbKey}", tbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> UnbindDeviceAsync(int tbKey)
    {
        try { return await _trackerRepo.UnbindAsync(tbKey); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解除裝置綁定失敗 TbKey={TbKey}", tbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> UnbindAllByMemberAsync(int memberTbKey)
    {
        try { return await _trackerRepo.UnbindAllByMemberAsync(memberTbKey); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解除會員所有裝置失敗 MemberTbKey={Key}", memberTbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<int> BatchDeleteByKeysAsync(IEnumerable<int> tbKeys)
    {
        try { return await _trackerRepo.BatchDeleteByKeysAsync(tbKeys); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批次刪除失敗");
            return 0;
        }
    }

    public async Task<OperationResult> BatchDeleteDevicesByImeiAsync(IEnumerable<string> imeis, int subAdminUserTbKey)
    {
        var list = imeis.Select(i => i.Trim()).Where(i => i.Length == 15 && i.All(char.IsDigit)).Distinct().ToList();
        if (list.Count == 0)
            return OperationResult.Fail("NO_IMEI");

        var errorLines = new List<string>();
        var deleted = 0;
        foreach (var imei in list)
        {
            var (success, code) = await _trackerRepo.DeleteDeviceByImeiAsync(imei, subAdminUserTbKey, "Delete Drive");
            if (success)
                deleted++;
            else
                errorLines.Add($"{imei}:{code ?? "ERROR"}");
        }

        if (deleted == 0 && errorLines.Count > 0)
            return OperationResult.Fail(string.Join("\n", errorLines));

        var msg = deleted.ToString();
        if (errorLines.Count > 0)
            msg += "|" + string.Join("\n", errorLines);
        return OperationResult.Ok(msg);
    }

    public async Task<OperationResult> DeleteDeviceByImeiAsync(string imei, int subAdminUserTbKey)
    {
        imei = (imei ?? string.Empty).Trim();
        if (imei.Length != 15 || !imei.All(char.IsDigit))
            return OperationResult.Fail("INVALID_IMEI");

        var (success, code) = await _trackerRepo.DeleteDeviceByImeiAsync(imei, subAdminUserTbKey, "刪除裝置");
        if (success)
            return OperationResult.Ok("OK");
        return OperationResult.Fail(code ?? "ERROR");
    }
}
