using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Repositories;

public interface IPayLogRepository
{
    Task<PayLog?> GetByIdAsync(int tbKey);
    Task<IEnumerable<PayLog>> GetByIMEIAsync(string imei);
    Task<IEnumerable<PayLog>> GetByMemberAsync(int memberTbKey);
    Task<PagedResult<PayLog>> GetPagedAsync(QueryFilter filter);
    Task<int> CreateAsync(PayLog payLog);
    Task<bool> UpdateAsync(PayLog payLog);
    Task<bool> DeleteAsync(int tbKey);
    Task<PayLog?> GetLatestByIMEIAsync(string imei);
    Task<IEnumerable<PayLog>> GetRenewalListAsync(DateTime startDate, DateTime endDate);
    Task<decimal> GetTotalAmountByOBMAsync(int obmTbKey, DateTime startDate, DateTime endDate);
    Task<int> GetCountAsync(QueryFilter filter);
}
