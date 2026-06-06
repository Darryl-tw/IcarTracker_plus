using Dapper;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Models;
using TrackerPlus.Repository.Infrastructure;

namespace TrackerPlus.Repository.Repositories;

public class ReportFieldsRepository : IReportFieldsRepository
{
    private readonly IDbConnectionFactory _db;

    public ReportFieldsRepository(IDbConnectionFactory db) => _db = db;

    public async Task<ReportFieldsDto?> GetByMemberAsync(int memberTbKey)
    {
        const string sql = "SELECT Voltage, GPS, LBS, GSM, Memo FROM dbo.ReportFields WHERE Member_tbKey = @MemberTbKey";
        using var conn = _db.CreateMainConnection();
        var row = await conn.QuerySingleOrDefaultAsync(sql, new { MemberTbKey = memberTbKey });
        if (row == null) return null;
        return new ReportFieldsDto
        {
            Voltage = ((string)row.Voltage) == "Y",
            Gps     = ((string)row.GPS) == "Y",
            Lbs     = ((string)row.LBS) == "Y",
            Gsm     = ((string)row.GSM) == "Y",
            Memo    = ((string)row.Memo) == "Y"
        };
    }

    public async Task<bool> UpsertAsync(int memberTbKey, ReportFieldsDto dto)
    {
        using var conn = _db.CreateMainConnection();
        var exists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.ReportFields WHERE Member_tbKey = @MemberTbKey",
            new { MemberTbKey = memberTbKey }) > 0;

        var p = new
        {
            MemberTbKey = memberTbKey,
            Voltage = dto.Voltage ? "Y" : "N",
            GPS = dto.Gps ? "Y" : "N",
            LBS = dto.Lbs ? "Y" : "N",
            GSM = dto.Gsm ? "Y" : "N",
            Memo = dto.Memo ? "Y" : "N"
        };

        if (exists)
        {
            return await conn.ExecuteAsync(
                "UPDATE dbo.ReportFields SET Voltage=@Voltage, GPS=@GPS, LBS=@LBS, GSM=@GSM, Memo=@Memo WHERE Member_tbKey=@MemberTbKey",
                p) > 0;
        }
        await conn.ExecuteAsync(
            "INSERT INTO dbo.ReportFields (Member_tbKey, Voltage, GPS, LBS, GSM, Memo) VALUES (@MemberTbKey,@Voltage,@GPS,@LBS,@GSM,@Memo)",
            p);
        return true;
    }
}
