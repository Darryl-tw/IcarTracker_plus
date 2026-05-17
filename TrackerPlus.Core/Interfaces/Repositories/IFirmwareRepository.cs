using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Repositories;

public interface IFirmwareRepository
{
    Task<FirmwareVersion?> GetByVersionAsync(string fwVersion);
    Task<IEnumerable<FirmwareVersion>> GetAllAsync();
    Task<PagedResult<FirmwareVersion>> GetPagedAsync(QueryFilter filter);
    Task<bool> CreateAsync(FirmwareVersion firmware);
    Task<bool> UpdateAsync(FirmwareVersion firmware, string originalFwVersion);
    Task<bool> DeleteAsync(string fwVersion);
    Task<bool> QueueFirmwareUpdateAsync(string targetFwVersion, IEnumerable<string> imeiList);
}
