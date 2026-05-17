using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Services;

public interface IAuthService
{
    Task<(bool Success, Member? Member, string ErrorMessage)> LoginAsync(string loginId, string password);
    Task<(bool Success, Member? Member, string ErrorMessage)> LoginByGoogleAsync(string googleToken);
    Task<(bool Success, Member? Member, string ErrorMessage)> LoginByAppleAsync(string appleToken);
    Task<bool> LogoutAsync(int memberTbKey);
    Task<bool> ChangePasswordAsync(int memberTbKey, string oldPassword, string newPassword);
    Task<bool> ResetPasswordAsync(string email, string newPassword, string verifyCode);
    Task<bool> SendPasswordResetEmailAsync(string email);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);
    /// <summary>後台管理員驗證</summary>
    Task<(bool Success, string ErrorMessage)> AdminLoginAsync(string account, string password);
}
