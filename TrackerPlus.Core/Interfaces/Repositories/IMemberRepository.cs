using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Repositories;

public interface IMemberRepository
{
    Task<Member?> GetByIdAsync(int tbKey);
    Task<Member?> GetByLoginIdAsync(string loginId);
    Task<Member?> GetByEmailAsync(string email);
    Task<PagedResult<Member>> GetPagedAsync(QueryFilter filter);
    Task<IEnumerable<Member>> GetAllAsync();
    Task<int> CreateAsync(Member member);
    Task<bool> UpdateAsync(Member member);
    Task<bool> DeleteAsync(int tbKey);
    Task<bool> UpdateStatusAsync(int tbKey, string status);
    Task<bool> UpdateLanguageAsync(int tbKey, string language);
    Task<bool> UpdatePasswordAsync(int tbKey, string hashedPassword);
    Task<bool> UpdateSystemParamsAsync(int tbKey, string userUnit, int timeZoneTbKey);
    Task<bool> UpdateProfileAsync(int tbKey, string cName, string tel, string addr, bool isSendEmail, bool isPush);
    Task<int> GetTimeZoneTbKeyAsync(int tbKey);
    Task<int> GetCountAsync(QueryFilter filter);
    Task<bool> ExistsAsync(string loginId);
}
