using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Services;

public interface IPayLogService
{
    Task<PayLog?> GetPayLogAsync(int tbKey);
    Task<IEnumerable<PayLog>> GetPayLogsByIMEIAsync(string imei);
    Task<IEnumerable<PayLog>> GetPayLogsByMemberAsync(int memberTbKey);
    Task<PagedResult<PayLog>> GetPayLogsPagedAsync(QueryFilter filter);
    Task<OperationResult> CreatePayLogAsync(PayLog payLog);
    Task<OperationResult> UpdatePayLogAsync(PayLog payLog);
    Task<OperationResult> DeletePayLogAsync(int tbKey);
    Task<OperationResult> MovePayLogAsync(int tbKey, int newMemberTbKey);
    Task<OperationResult> MovePayLogByImeiAsync(int tbKey, string newImei);
    Task<IEnumerable<PayLog>> GetRenewalListAsync(DateTime startDate, DateTime endDate);
    Task<byte[]> ExportRenewalToExcelAsync(DateTime startDate, DateTime endDate);
    Task<int> GetPayLogCountAsync(QueryFilter filter);
}
