using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Services.Services;

public class FirmwareService : IFirmwareService
{
    private readonly IFirmwareRepository _firmwareRepo;
    private readonly ILogger<FirmwareService> _logger;

    public FirmwareService(IFirmwareRepository firmwareRepo, ILogger<FirmwareService> logger)
    {
        _firmwareRepo = firmwareRepo;
        _logger = logger;
    }

    public Task<FirmwareVersion?> GetFirmwareAsync(string fwVersion)
        => _firmwareRepo.GetByVersionAsync(fwVersion);

    public Task<IEnumerable<FirmwareVersion>> GetAllFirmwaresAsync()
        => _firmwareRepo.GetAllAsync();

    public Task<PagedResult<FirmwareVersion>> GetFirmwaresPagedAsync(QueryFilter filter)
        => _firmwareRepo.GetPagedAsync(filter);

    public async Task<OperationResult> CreateFirmwareAsync(FirmwareVersion firmware)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(firmware.FWVERSION))
                return OperationResult.Fail("韌體版本不可為空");
            var ok = await _firmwareRepo.CreateAsync(firmware);
            return ok ? OperationResult.Ok("新增成功") : OperationResult.Fail("新增失敗");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "新增韌體失敗");
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> UpdateFirmwareAsync(FirmwareVersion firmware, string originalFwVersion)
    {
        try
        {
            var ok = await _firmwareRepo.UpdateAsync(firmware, originalFwVersion);
            return ok ? OperationResult.Ok("更新成功") : OperationResult.Fail("找不到韌體");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新韌體失敗");
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteFirmwareAsync(string fwVersion)
    {
        try
        {
            var ok = await _firmwareRepo.DeleteAsync(fwVersion);
            return ok ? OperationResult.Ok("刪除成功") : OperationResult.Fail("找不到韌體");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除韌體失敗");
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> BatchQueueFirmwareUpdateAsync(string targetFwVersion, IEnumerable<string> imeiList)
    {
        try
        {
            var fw = await _firmwareRepo.GetByVersionAsync(targetFwVersion);
            if (fw == null) return OperationResult.Fail("找不到韌體版本");

            var list = imeiList.ToList();
            if (list.Count == 0) return OperationResult.Fail("未提供 IMEI");

            var ok = await _firmwareRepo.QueueFirmwareUpdateAsync(targetFwVersion, list);
            return ok ? OperationResult.Ok($"已排程 {list.Count} 筆裝置升級至 {targetFwVersion}", list.Count) : OperationResult.Fail("排程失敗");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批次韌體升級失敗");
            return OperationResult.Fail(ex.Message);
        }
    }
}
