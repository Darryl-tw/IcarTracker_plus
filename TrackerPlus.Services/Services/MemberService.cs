using BCrypt.Net;
using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Services.Services;

public class MemberService : IMemberService
{
    private readonly IMemberRepository _memberRepo;
    private readonly ILogger<MemberService> _logger;

    public MemberService(IMemberRepository memberRepo, ILogger<MemberService> logger)
    {
        _memberRepo = memberRepo;
        _logger = logger;
    }

    public async Task<Member?> GetMemberAsync(int tbKey)
        => await _memberRepo.GetByIdAsync(tbKey);

    public async Task<Member?> GetMemberByLoginIdAsync(string loginId)
        => await _memberRepo.GetByLoginIdAsync(loginId);

    public async Task<PagedResult<Member>> GetMembersAsync(QueryFilter filter)
        => await _memberRepo.GetPagedAsync(filter);

    public async Task<OperationResult> CreateMemberAsync(Member member)
    {
        try
        {
            if (await _memberRepo.ExistsAsync(member.ID))
                return OperationResult.Fail("帳號已存在", "DUPLICATE_ID");
            member.CDate = DateTime.UtcNow;
            var newKey = await _memberRepo.CreateAsync(member);
            return OperationResult.Ok("新增成功", newKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "新增會員失敗 ID={ID}", member.ID);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> UpdateMemberAsync(Member member)
    {
        try
        {
            var ok = await _memberRepo.UpdateAsync(member);
            return ok ? OperationResult.Ok("更新成功") : OperationResult.Fail("找不到會員");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新會員失敗 TbKey={TbKey}", member.TbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteMemberAsync(int tbKey)
    {
        try
        {
            var ok = await _memberRepo.DeleteAsync(tbKey);
            return ok ? OperationResult.Ok("刪除成功") : OperationResult.Fail("找不到會員");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除會員失敗 TbKey={TbKey}", tbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> SetMemberStatusAsync(int tbKey, string status)
    {
        try
        {
            var ok = await _memberRepo.UpdateStatusAsync(tbKey, status);
            return ok ? OperationResult.Ok("狀態更新成功") : OperationResult.Fail("找不到會員");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "設定會員狀態失敗 TbKey={TbKey}", tbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> RegisterMemberAsync(Member member, string password)
    {
        try
        {
            if (await _memberRepo.ExistsAsync(member.ID))
                return OperationResult.Fail("帳號已存在", "DUPLICATE_ID");
            member.Password = BCrypt.Net.BCrypt.HashPassword(password);
            member.MemberStatus = "N";
            member.CDate = DateTime.UtcNow;
            member.DynaCheckCode = Guid.NewGuid().ToString("N")[..8].ToUpper();
            var newKey = await _memberRepo.CreateAsync(member);
            return OperationResult.Ok("註冊成功", newKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "會員註冊失敗 ID={ID}", member.ID);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<bool> VerifyEmailAsync(string email, string code)
    {
        var member = await _memberRepo.GetByEmailAsync(email);
        if (member == null) return false;
        if (!string.Equals(member.DynaCheckCode, code, StringComparison.OrdinalIgnoreCase)) return false;
        await _memberRepo.UpdateStatusAsync(member.TbKey, "Y");
        return true;
    }

    public async Task<IEnumerable<Member>> GetMembersByOBMAsync(int obmTbKey)
    {
        var filter = new QueryFilter { PageSize = 9999 };
        var result = await _memberRepo.GetPagedAsync(filter);
        return result.Items.Where(m => m.OBM_TbKey == obmTbKey);
    }

    public async Task<int> GetMemberCountAsync(QueryFilter filter)
        => await _memberRepo.GetCountAsync(filter);

    public Task<bool> UpdateMemberLanguageAsync(int tbKey, string userLanguage)
        => _memberRepo.UpdateLanguageAsync(tbKey, userLanguage);

    public async Task<OperationResult> ChangePasswordAsync(int tbKey, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
            return OperationResult.Fail("密碼不可為空");
        try
        {
            var hashed = BCrypt.Net.BCrypt.HashPassword(newPassword);
            var ok = await _memberRepo.UpdatePasswordAsync(tbKey, hashed);
            return ok ? OperationResult.Ok("密碼已更新") : OperationResult.Fail("更新失敗");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更改密碼失敗 TbKey={TbKey}", tbKey);
            return OperationResult.Fail(ex.Message);
        }
    }
}
