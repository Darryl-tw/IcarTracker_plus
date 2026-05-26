using Dapper;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Repository.Infrastructure;

namespace TrackerPlus.Repository.Repositories;

public class AdminUserRepository : IAdminUserRepository
{
    private readonly IDbConnectionFactory _db;

    public AdminUserRepository(IDbConnectionFactory db) => _db = db;

    public async Task<bool> ValidateAsync(string userId, string password)
    {
        const string sql = "SELECT COUNT(*) FROM dbo.Userdb WHERE UserID = @UserID AND UserPW = @UserPW";
        using var conn = _db.CreateMainConnection();
        var count = await conn.ExecuteScalarAsync<int>(sql, new { UserID = userId.Trim(), UserPW = password.Trim() });
        return count > 0;
    }
}
