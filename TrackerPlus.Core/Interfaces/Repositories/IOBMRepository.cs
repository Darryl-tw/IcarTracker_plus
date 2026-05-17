using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Repositories;

public interface IOBMRepository
{
    Task<OBM?> GetByIdAsync(int tbKey);
    Task<OBM?> GetByDomainAsync(string domain);
    Task<IEnumerable<OBM>> GetAllAsync();
    Task<PagedResult<OBM>> GetPagedAsync(QueryFilter filter);
    Task<int> CreateAsync(OBM obm);
    Task<bool> UpdateAsync(OBM obm);
    Task<bool> DeleteAsync(int tbKey);
}
