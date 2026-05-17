using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Repositories;

public interface IWebServiceIPRepository
{
    Task<IEnumerable<WebServiceIP>> GetAllAsync();
    Task<PagedResult<WebServiceIP>> GetPagedAsync(QueryFilter filter);
    Task<WebServiceIP?> GetByIdAsync(int tbKey);
    Task<int> CreateAsync(WebServiceIP ip);
    Task<bool> DeleteAsync(int tbKey);
    Task<bool> IsAllowedAsync(string ipAddress);
}
