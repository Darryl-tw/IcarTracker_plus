using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Services.Services;

public class AuthService : IAuthService
{
    private readonly IMemberRepository _memberRepo;
    private readonly IAdminUserRepository _adminUserRepo;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IMemberRepository memberRepo, IAdminUserRepository adminUserRepo, ILogger<AuthService> logger)
    {
        _memberRepo = memberRepo;
        _adminUserRepo = adminUserRepo;
        _logger = logger;
    }

    public async Task<(bool Success, Member? Member, string ErrorMessage)> LoginAsync(string loginId, string password)
    {
        try
        {
            var member = await _memberRepo.GetByLoginIdAsync(loginId);
            if (member == null)
                return (false, null, "帳號或密碼錯誤");
            if (member.MemberStatus != "Y")
                return (false, null, "帳號已停用");
            if (!VerifyPassword(password, member.Password))
                return (false, null, "帳號或密碼錯誤");
            return (true, member, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登入失敗 ID={ID}", loginId);
            return (false, null, "系統錯誤");
        }
    }

    public async Task<(bool Success, Member? Member, string ErrorMessage)> LoginByEmailAsync(string email)
    {
        try
        {
            var member = await _memberRepo.GetByEmailAsync(email);
            if (member == null)
                return (false, null, "此電子郵件尚未註冊帳號");
            if (member.MemberStatus != "Y")
                return (false, null, "帳號已停用");
            return (true, member, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email 登入失敗 Email={Email}", email);
            return (false, null, "系統錯誤");
        }
    }

    public Task<(bool Success, Member? Member, string ErrorMessage)> LoginByGoogleAsync(string googleToken)
        => Task.FromResult<(bool, Member?, string)>((false, null, "尚未實作"));

    public Task<(bool Success, Member? Member, string ErrorMessage)> LoginByAppleAsync(string appleToken)
        => Task.FromResult<(bool, Member?, string)>((false, null, "尚未實作"));

    public Task<bool> LogoutAsync(int memberTbKey)
        => Task.FromResult(true);

    public async Task<bool> ChangePasswordAsync(int memberTbKey, string oldPassword, string newPassword)
    {
        try
        {
            var member = await _memberRepo.GetByIdAsync(memberTbKey);
            if (member == null) return false;
            if (!VerifyPassword(oldPassword, member.Password)) return false;
            var hashed = HashPassword(newPassword);
            return await _memberRepo.UpdatePasswordAsync(memberTbKey, hashed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "變更密碼失敗 TbKey={TbKey}", memberTbKey);
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(string email, string newPassword, string verifyCode)
    {
        try
        {
            var member = await _memberRepo.GetByEmailAsync(email);
            if (member == null) return false;
            if (!string.Equals(member.DynaCheckCode, verifyCode, StringComparison.OrdinalIgnoreCase)) return false;
            var hashed = HashPassword(newPassword);
            return await _memberRepo.UpdatePasswordAsync(member.TbKey, hashed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重設密碼失敗 Email={Email}", email);
            return false;
        }
    }

    public Task<bool> SendPasswordResetEmailAsync(string email)
    {
        _logger.LogInformation("送出重設密碼信 Email={Email}", email);
        return Task.FromResult(true);
    }

    public string HashPassword(string password)
        => BCrypt.Net.BCrypt.HashPassword(password);

    public bool VerifyPassword(string password, string hashedPassword)
    {
        var input = password.Trim();
        var stored = (hashedPassword ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(stored)) return false;

        try
        {
            if (stored.StartsWith("$2", StringComparison.Ordinal))
                return BCrypt.Net.BCrypt.Verify(input, stored);
        }
        catch
        {
            // fall through to legacy compare
        }

        // 相容舊版 Tracker：密碼以 char 欄位明文儲存
        return string.Equals(input, stored, StringComparison.Ordinal);
    }

    public async Task<(bool Success, string ErrorMessage)> AdminLoginAsync(string account, string password)
    {
        if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(password))
            return (false, "帳號或密碼錯誤");

        try
        {
            if (await _adminUserRepo.ValidateAsync(account, password))
                return (true, string.Empty);
            return (false, "帳號或密碼錯誤");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "後台 Userdb 驗證失敗 UserID={UserId}", account);
            return (false, "系統錯誤");
        }
    }
}
                                                      