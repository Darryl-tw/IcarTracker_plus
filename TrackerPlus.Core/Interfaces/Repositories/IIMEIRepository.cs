using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Repositories;

public interface IIMEIRepository
{
    Task<IMEIDevice?> GetByIdAsync(int tbKey);
    Task<IMEIDevice?> GetByIMEIAsync(string imei);
    Task<PagedResult<IMEIDevice>> GetPagedAsync(QueryFilter filter);
    Task<IEnumerable<IMEIDevice>> GetByMemberAsync(int memberTbKey);
    Task<int> CreateAsync(IMEIDevice device);
    Task<bool> UpdateAsync(IMEIDevice device);
    Task<bool> DeleteAsync(int tbKey);
    Task<bool> BatchDeleteAsync(IEnumerable<string> imeiList);
    Task<bool> MoveToMemberAsync(string imei, int newMemberTbKey, int operatorTbKey);
    Task<bool> ResetAsync(string imei);
    Task<bool> FullResetAsync(string imei, IMEIFullResetOptions options);
    Task<bool> ExistsAsync(string imei);
    Task<int> GetCountAsync(QueryFilter filter);
    Task<IEnumerable<IMEIDevice>> GetPendingFirmwareUpdateAsync();
}
