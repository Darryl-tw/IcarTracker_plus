using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Services;

public interface IFirmwareService
{
    Task<FirmwareVersion?> GetFirmwareAsync(string fwVersion);
    Task<IEnumerable<FirmwareVersion>> GetAllFirmwaresAsync();
    Task<PagedResult<FirmwareVersion>> GetFirmwaresPagedAsync(QueryFilter filter);
    Task<OperationResult> CreateFirmwareAsync(FirmwareVersion firmware);
    Task<OperationResult> UpdateFirmwareAsync(FirmwareVersion firmware, string originalFwVersion);
    Task<OperationResult> DeleteFirmwareAsync(string fwVersion);
    Task<OperationResult> BatchQueueFirmwareUpdateAsync(string targetFwVersion, IEnumerable<string> imeiList);
}
