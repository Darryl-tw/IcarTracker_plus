using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Repositories;

public interface IDealerRepository
{
    Task<IEnumerable<Dealer>> GetAllAsync(string? keyword, string? status);
    Task<Dealer?> GetByIdAsync(int tbKey);
    Task<bool> UpdateAsync(Dealer dealer);
    Task<int> CreateAsync(Dealer dealer);
    Task<bool> DeleteAsync(int tbKey);
}
