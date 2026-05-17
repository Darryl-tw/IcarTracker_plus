using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Services;

public interface IOBMService
{
    Task<OBM?> GetOBMAsync(int tbKey);
    Task<IEnumerable<OBM>> GetAllOBMsAsync();
    Task<PagedResult<OBM>> GetOBMsPagedAsync(QueryFilter filter);
    Task<OperationResult> CreateOBMAsync(OBM obm);
    Task<OperationResult> UpdateOBMAsync(OBM obm);
    Task<OperationResult> DeleteOBMAsync(int tbKey);
    Task<(string Title, string Logo, string Contact)> GetDomainInfoAsync(string domain);
}
