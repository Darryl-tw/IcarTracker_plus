using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Services;

public interface IDealerService
{
    Task<IEnumerable<Dealer>> GetDealersAsync(string? keyword, string? status);
    Task<Dealer?> GetDealerAsync(int tbKey);
    Task<OperationResult> UpdateDealerAsync(Dealer dealer);
    Task<OperationResult> CreateDealerAsync(Dealer dealer);
    Task<OperationResult> DeleteDealerAsync(int tbKey);
}
