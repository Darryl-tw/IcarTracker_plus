using Dapper;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Repository.Infrastructure;

namespace TrackerPlus.Repository.Repositories;

public class SubAdminRepository : ISubAdminRepository
{
    private readonly IDbConnectionFactory _db;

    public SubAdminRepository(IDbConnectionFactory db) => _db = db;

    public async Task<int?> ValidateMoveDeviceAsync(string subAdminId, string subAdminPassword, bool defaultPay, int saleModel)
    {
        if (string.IsNullOrWhiteSpace(subAdminId) || string.IsNullOrWhiteSpace(subAdminPassword))
            return null;

        var sql = defaultPay switch
        {
            true when saleModel == 1 =>
                @"SELECT tbKey FROM dbo.Sub_adminUser
                  WHERE Sub_adminID=@Id AND Sub_adminPW=@Pw AND Enable=1
                    AND MoveDevice=1 AND DefaultPayLog=1",
            true =>
                @"SELECT tbKey FROM dbo.Sub_adminUser
                  WHERE Sub_adminID=@Id AND Sub_adminPW=@Pw AND Enable=1
                    AND MoveDevice=1 AND PayLog=1",
            _ =>
                @"SELECT tbKey FROM dbo.Sub_adminUser
                  WHERE Sub_adminID=@Id AND Sub_adminPW=@Pw AND Enable=1
                    AND MoveDevice=1"
        };

        using var conn = _db.CreateMainConnection();
        var key = await conn.ExecuteScalarAsync<int?>(sql, new { Id = subAdminId.Trim(), Pw = subAdminPassword });
        return key is > 0 ? key : null;
    }

    public async Task<int?> ValidateDeleteDeviceAsync(string subAdminId, string subAdminPassword)
    {
        if (string.IsNullOrWhiteSpace(subAdminId) || string.IsNullOrWhiteSpace(subAdminPassword))
            return null;

        const string sql = @"SELECT tbKey FROM dbo.Sub_adminUser
            WHERE Sub_adminID=@Id AND Sub_adminPW=@Pw AND Enable=1 AND DelDevice=1";

        using var conn = _db.CreateMainConnection();
        var key = await conn.ExecuteScalarAsync<int?>(sql, new { Id = subAdminId.Trim(), Pw = subAdminPassword });
        return key is > 0 ? key : null;
    }

    public async Task<int?> ValidateInsertDeviceAsync(string subAdminId, string subAdminPassword)
    {
        if (string.IsNullOrWhiteSpace(subAdminId) || string.IsNullOrWhiteSpace(subAdminPassword))
            return null;

        const string sql = @"SELECT tbKey FROM dbo.Sub_adminUser
            WHERE Sub_adminID=@Id AND Sub_adminPW=@Pw AND Enable=1 AND InsertDevice=1";

        using var conn = _db.CreateMainConnection();
        var key = await conn.ExecuteScalarAsync<int?>(sql, new { Id = subAdminId.Trim(), Pw = subAdminPassword });
        return key is > 0 ? key : null;
    }

    public async Task<int?> ValidateEditPayLogAsync(string subAdminId, string subAdminPassword)
    {
        if (string.IsNullOrWhiteSpace(subAdminId) || string.IsNullOrWhiteSpace(subAdminPassword))
            return null;

        const string sql = @"SELECT tbKey FROM dbo.Sub_adminUser
            WHERE Sub_adminID=@Id AND Sub_adminPW=@Pw AND Enable=1 AND Edit_PayLog=1";

        using var conn = _db.CreateMainConnection();
        var key = await conn.ExecuteScalarAsync<int?>(sql, new { Id = subAdminId.Trim(), Pw = subAdminPassword });
        return key is > 0 ? key : null;
    }
}
