using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Services;

public interface IWebServiceIPService
{
    Task<IEnumerable<WebServiceIP>> GetAllAsync();
    Task<PagedResult<WebServiceIP>> GetPagedAsync(QueryFilter filter);
    Task<OperationResult> CreateAsync(WebServiceIP ip);
    Task<OperationResult> DeleteAsync(int tbKey);
    Task<bool> IsAllowedAsync(string ipAddress);
}
