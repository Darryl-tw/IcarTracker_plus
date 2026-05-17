using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Services.Services;

public class IMEIService : IIMEIService
{
    private readonly IIMEIRepository _imeiRepo;
    private readonly ILogger<IMEIService> _logger;

    public IMEIService(IIMEIRepository imeiRepo, ILogger<IMEIService> logger)
    {
        _imeiRepo = imeiRepo;
        _logger = logger;
    }

    public async Task<IMEIDevice?> GetIMEIAsync(int tbKey)
        => await _imeiRepo.GetByIdAsync(tbKey);

    public async Task<IMEIDevice?> GetIMEIByCodeAsync(string imei)
        => await _imeiRepo.GetByIMEIAsync(imei);

    public async Task<PagedResult<IMEIDevice>> GetIMEIsPagedAsync(QueryFilter filter)
        => await _imeiRepo.GetPagedAsync(filter);

    public async Task<OperationResult> RegisterIMEIAsync(IMEIDevice device)
    {
        try
        {
            if (!await ValidateIMEIFormatAsync(device.IMEICODE))
                return OperationResult.Fail("IMEI 格式錯誤，需為 15 位數字", "INVALID_IMEI");
            if (await _imeiRepo.ExistsAsync(device.IMEICODE))
                return OperationResult.Fail("IMEI 已存在", "DUPLICATE_IMEI");
            var newKey = await _imeiRepo.CreateAsync(device);
            return OperationResult.Ok("新增成功", newKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "新增 IMEI 失敗 {IMEI}", device.IMEICODE);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> UpdateIMEIAsync(IMEIDevice device)
    {
        try
        {
            var ok = await _imeiRepo.UpdateAsync(device);
            return ok ? OperationResult.Ok("更新成功") : OperationResult.Fail("找不到 IMEI");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新 IMEI 失敗 TbKey={TbKey}", device.TbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteIMEIAsync(int tbKey)
    {
        try
        {
            var ok = await _imeiRepo.DeleteAsync(tbKey);
            return ok ? OperationResult.Ok("刪除成功") : OperationResult.Fail("找不到 IMEI");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除 IMEI 失敗 TbKey={TbKey}", tbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> BatchDeleteIMEIAsync(IEnumerable<string> imeiList)
    {
        try
        {
            var list = imeiList.ToList();
            if (!list.Any()) return OperationResult.Fail("未選取任何 IMEI");
            var ok = await _imeiRepo.BatchDeleteAsync(list);
            return ok ? OperationResult.Ok($"已刪除 {list.Count} 筆") : OperationResult.Fail("刪除失敗");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批次刪除 IMEI 失敗");
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> MoveIMEIAsync(string imei, int newMemberTbKey, int operatorTbKey)
    {
        try
        {
            var ok = await _imeiRepo.MoveToMemberAsync(imei, newMemberTbKey, operatorTbKey);
            return ok ? OperationResult.Ok("移轉成功") : OperationResult.Fail("移轉失敗");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移轉 IMEI 失敗 {IMEI}", imei);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> ResetIMEIAsync(string imei)
    {
        try
        {
            var ok = await _imeiRepo.ResetAsync(imei);
            return ok ? OperationResult.Ok("已解除會員綁定") : OperationResult.Fail("重置失敗");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重置 IMEI 失敗 {IMEI}", imei);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> FullResetIMEIAsync(string imei, IMEIFullResetOptions options)
    {
        try
        {
            if (!await ValidateIMEIFormatAsync(imei))
                return OperationResult.Fail("IMEI 格式錯誤", "INVALID_IMEI");
            if (!await _imeiRepo.ExistsAsync(imei))
                return OperationResult.Fail("IMEI 不存在", "NOT_FOUND");

            var ok = await _imeiRepo.FullResetAsync(imei, options);
            return ok ? OperationResult.Ok("裝置完整重置成功") : OperationResult.Fail("完整重置失敗");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完整重置 IMEI 失敗 {IMEI}", imei);
            return OperationResult.Fail(ex.Message);
        }
    }

    public Task<bool> ValidateIMEIFormatAsync(string imei)
        => Task.FromResult(!string.IsNullOrWhiteSpace(imei)
            && imei.Length == 15
            && imei.All(char.IsDigit));

    public async Task<int> GetIMEICountAsync(QueryFilter filter)
        => await _imeiRepo.GetCountAsync(filter);
}
