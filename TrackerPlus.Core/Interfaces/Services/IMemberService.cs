using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Services;

public interface IMemberService
{
    Task<Member?> GetMemberAsync(int tbKey);
    Task<Member?> GetMemberByLoginIdAsync(string loginId);
    Task<PagedResult<Member>> GetMembersAsync(QueryFilter filter);
    Task<OperationResult> CreateMemberAsync(Member member);
    Task<OperationResult> UpdateMemberAsync(Member member);
    Task<OperationResult> DeleteMemberAsync(int tbKey);
    Task<OperationResult> SetMemberStatusAsync(int tbKey, string status);
    Task<OperationResult> RegisterMemberAsync(Member member, string password);
    Task<bool> VerifyEmailAsync(string email, string code);
    Task<IEnumerable<Member>> GetMembersByOBMAsync(int obmTbKey);
    Task<int> GetMemberCountAsync(QueryFilter filter);
    Task<bool> UpdateMemberLanguageAsync(int tbKey, string userLanguage);
}
