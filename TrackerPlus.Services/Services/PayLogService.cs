using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Services.Services;

public class PayLogService : IPayLogService
{
    private readonly IPayLogRepository _payLogRepo;
    private readonly ILogger<PayLogService> _logger;

    public PayLogService(IPayLogRepository payLogRepo, ILogger<PayLogService> logger)
    {
        _payLogRepo = payLogRepo;
        _logger = logger;
    }

    public async Task<PayLog?> GetPayLogAsync(int tbKey)
        => await _payLogRepo.GetByIdAsync(tbKey);

    public async Task<IEnumerable<PayLog>> GetPayLogsByIMEIAsync(string imei)
        => await _payLogRepo.GetByIMEIAsync(imei);

    public async Task<IEnumerable<PayLog>> GetPayLogsByMemberAsync(int memberTbKey)
        => await _payLogRepo.GetByMemberAsync(memberTbKey);

    public async Task<PagedResult<PayLog>> GetPayLogsPagedAsync(QueryFilter filter)
        => await _payLogRepo.GetPagedAsync(filter);

    public async Task<OperationResult> CreatePayLogAsync(PayLog payLog)
    {
        try
        {
            payLog.CDate = DateTime.UtcNow;
            var newKey = await _payLogRepo.CreateAsync(payLog);
            return OperationResult.Ok("新增成功", newKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "新增付款記錄失敗 IMEI={IMEI}", payLog.IMEICODE);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> UpdatePayLogAsync(PayLog payLog)
    {
        try
        {
            var ok = await _payLogRepo.UpdateAsync(payLog);
            return ok ? OperationResult.Ok("更新成功") : OperationResult.Fail("找不到記錄");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新付款記錄失敗 TbKey={TbKey}", payLog.TbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> DeletePayLogAsync(int tbKey)
    {
        try
        {
            var ok = await _payLogRepo.DeleteAsync(tbKey);
            return ok ? OperationResult.Ok("刪除成功") : OperationResult.Fail("找不到記錄");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除付款記錄失敗 TbKey={TbKey}", tbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> MovePayLogAsync(int tbKey, int newMemberTbKey)
    {
        try
        {
            var payLog = await _payLogRepo.GetByIdAsync(tbKey);
            if (payLog == null) return OperationResult.Fail("找不到記錄");
            payLog.Member_TbKey = newMemberTbKey;
            var ok = await _payLogRepo.UpdateAsync(payLog);
            return ok ? OperationResult.Ok("移轉成功") : OperationResult.Fail("移轉失敗");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移轉付款記錄失敗 TbKey={TbKey}", tbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> MovePayLogByImeiAsync(int tbKey, string newImei)
    {
        try
        {
            var ok = await _payLogRepo.MoveByImeiAsync(tbKey, newImei.Trim());
            return ok ? OperationResult.Ok("移轉成功") : OperationResult.Fail("找不到裝置或目標 IMEI 不存在");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "依 IMEI 移轉付款記錄失敗 TbKey={TbKey} NewImei={IMEI}", tbKey, newImei);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<IEnumerable<PayLog>> GetRenewalListAsync(DateTime startDate, DateTime endDate)
        => await _payLogRepo.GetRenewalListAsync(startDate, endDate);

    public Task<byte[]> ExportRenewalToExcelAsync(DateTime startDate, DateTime endDate)
    {
        // TODO: implement with ClosedXML
        return Task.FromResult(Array.Empty<byte>());
    }

    public async Task<int> GetPayLogCountAsync(QueryFilter filter)
        => await _payLogRepo.GetCountAsync(filter);
}
