using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Services.Services;

public class AuthService : IAuthService
{
    private readonly IMemberRepository _memberRepo;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IMemberRepository memberRepo, IConfiguration config, ILogger<AuthService> logger)
    {
        _memberRepo = memberRepo;
        _config = config;
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

    public Task<(bool Success, string ErrorMessage)> AdminLoginAsync(string account, string password)
    {
        var adminAccount = _config["AdminAuth:Account"];
        var adminPassword = _config["AdminAuth:Password"];
        if (string.IsNullOrEmpty(adminAccount))
            return Task.FromResult<(bool, string)>((false, "後台未設定管理員帳號"));
        if (!string.Equals(account, adminAccount, StringComparison.Ordinal))
            return Task.FromResult<(bool, string)>((false, "帳號或密碼錯誤"));
        if (!string.Equals(password, adminPassword, StringComparison.Ordinal))
            return Task.FromResult<(bool, string)>((false, "帳號或密碼錯誤"));
        return Task.FromResult<(bool, string)>((true, string.Empty));
    }
}
