using Dapper;
using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Models;
using TrackerPlus.Repository.Infrastructure;
using TrackerPlus.Repository.Mapping;

namespace TrackerPlus.Repository.Repositories;

public class PayLogRepository : IPayLogRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<PayLogRepository> _logger;

    public PayLogRepository(IDbConnectionFactory db, ILogger<PayLogRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PayLog?> GetByIdAsync(int tbKey)
    {
        var sql = $"{PayLogMapper.SelectSql} {PayLogMapper.FromSql} WHERE p.tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        var row = await conn.QuerySingleOrDefaultAsync<PayLogDbRow>(sql, new { TbKey = tbKey });
        return row == null ? null : PayLogMapper.ToModel(row);
    }

    public async Task<IEnumerable<PayLog>> GetByIMEIAsync(string imei)
    {
        var sql = $@"{PayLogMapper.SelectSql} {PayLogMapper.FromSql}
            WHERE RTRIM(ISNULL(p.IMEICode,'')) = @IMEI
               OR (p.Tracker_tbKey > 0 AND RTRIM(ISNULL(t.IMEICode,'')) = @IMEI)
            ORDER BY p.CDate ASC";
        using var conn = _db.CreateMainConnection();
        var rows = await conn.QueryAsync<PayLogDbRow>(sql, new { IMEI = imei.Trim() });
        return rows.Select(PayLogMapper.ToModel);
    }

    public async Task<IEnumerable<PayLog>> GetByMemberAsync(int memberTbKey)
    {
        var sql = $"{PayLogMapper.SelectSql} {PayLogMapper.FromSql} WHERE p.Member_tbKey = @MemberTbKey ORDER BY p.CDate DESC";
        using var conn = _db.CreateMainConnection();
        var rows = await conn.QueryAsync<PayLogDbRow>(sql, new { MemberTbKey = memberTbKey });
        return rows.Select(PayLogMapper.ToModel);
    }

    public async Task<PagedResult<PayLog>> GetPagedAsync(QueryFilter filter)
    {
        var offset = (filter.PageIndex - 1) * filter.PageSize;
        var where = BuildWhereClause(filter);

        var countSql = $"SELECT COUNT(*) {PayLogMapper.FromSql} {where}";
        var dataSql = $@"{PayLogMapper.SelectSql} {PayLogMapper.FromSql} {where}
            ORDER BY p.tbKey DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var param = new { filter.Keyword, filter.Status, filter.StartDate, filter.EndDate, Offset = offset, filter.PageSize };
        using var conn = _db.CreateMainConnection();
        var total = await conn.ExecuteScalarAsync<int>(countSql, param);
        var rows = await conn.QueryAsync<PayLogDbRow>(dataSql, param);

        return new PagedResult<PayLog>
        {
            Items = rows.Select(PayLogMapper.ToModel),
            TotalCount = total,
            PageIndex = filter.PageIndex,
            PageSize = filter.PageSize
        };
    }

    public async Task<int> CreateAsync(PayLog payLog)
    {
        _logger.LogInformation("新增付款記錄 IMEI={IMEI}", payLog.IMEICODE);
        const string sql = @"INSERT INTO dbo.PayLog
            (Member_tbKey, Tracker_tbKey, IMEICode, OrderNumber, Amount, SDate, EDate, CDate, CMemo, Model)
            VALUES
            (@Member_TbKey, @Tracker_tbKey, @IMEICode, @OrderNumber, @Amount, @SDate, @EDate, GETDATE(), @CMemo, @Model);
            SELECT CAST(SCOPE_IDENTITY() AS int);";

        using var conn = _db.CreateMainConnection();
        var trackerTbKey = await conn.ExecuteScalarAsync<int?>(
            "SELECT tbKey FROM dbo.Tracker WHERE RTRIM(IMEICode) = @IMEI",
            new { IMEI = payLog.IMEICODE.Trim() });

        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            Member_TbKey = payLog.Member_TbKey,
            Tracker_tbKey = trackerTbKey ?? 0,
            IMEICode = payLog.IMEICODE.Trim(),
            OrderNumber = payLog.OrderNo,
            payLog.Amount,
            payLog.SDate,
            payLog.EDate,
            CMemo = payLog.Memo,
            Model = payLog.SaleModel
        });
    }

    public async Task<bool> UpdateAsync(PayLog payLog)
    {
        const string sql = @"UPDATE dbo.PayLog SET
            Amount = @Amount, SDate = @SDate, EDate = @EDate, CMemo = @CMemo, Model = @Model
            WHERE tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, new
        {
            payLog.TbKey,
            payLog.Amount,
            payLog.SDate,
            payLog.EDate,
            CMemo = payLog.Memo,
            Model = payLog.SaleModel
        }) > 0;
    }

    public async Task<bool> DeleteAsync(int tbKey)
    {
        const string sql = "DELETE FROM dbo.PayLog WHERE tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, new { TbKey = tbKey }) > 0;
    }

    public async Task<PayLog?> GetLatestByIMEIAsync(string imei)
    {
        var sql = $"{PayLogMapper.SelectSql} {PayLogMapper.FromSql} WHERE RTRIM(p.IMEICode) = @IMEI ORDER BY p.EDate DESC";
        using var conn = _db.CreateMainConnection();
        var row = await conn.QueryFirstOrDefaultAsync<PayLogDbRow>(sql, new { IMEI = imei.Trim() });
        return row == null ? null : PayLogMapper.ToModel(row);
    }

    public async Task<IEnumerable<PayLog>> GetRenewalListAsync(DateTime startDate, DateTime endDate)
    {
        var sql = $@"{PayLogMapper.SelectSql} {PayLogMapper.FromSql}
            WHERE p.EDate >= @StartDate AND p.EDate <= @EndDate
            ORDER BY p.EDate ASC";
        using var conn = _db.CreateMainConnection();
        var rows = await conn.QueryAsync<PayLogDbRow>(sql, new { StartDate = startDate, EndDate = endDate });
        return rows.Select(PayLogMapper.ToModel);
    }

    public async Task<decimal> GetTotalAmountByOBMAsync(int obmTbKey, DateTime startDate, DateTime endDate)
    {
        const string sql = @"SELECT ISNULL(SUM(p.Amount), 0) FROM dbo.PayLog p
            INNER JOIN dbo.Tracker t ON p.Tracker_tbKey = t.tbKey
            WHERE t.OBM_tbKey = @OBMTbKey AND p.CDate >= @StartDate AND p.CDate <= @EndDate";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteScalarAsync<decimal>(sql, new { OBMTbKey = obmTbKey, StartDate = startDate, EndDate = endDate });
    }

    public async Task<int> GetCountAsync(QueryFilter filter)
    {
        var where = BuildWhereClause(filter);
        var sql = $"SELECT COUNT(*) {PayLogMapper.FromSql} {where}";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { filter.Keyword, filter.Status, filter.StartDate, filter.EndDate });
    }

    private static string BuildWhereClause(QueryFilter filter)
    {
        var conditions = new List<string>();
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
            conditions.Add("(RTRIM(p.IMEICode) LIKE '%' + @Keyword + '%' OR RTRIM(m.CName) LIKE '%' + @Keyword + '%' OR RTRIM(p.OrderNumber) LIKE '%' + @Keyword + '%')");
        if (filter.StartDate.HasValue)
            conditions.Add("p.CDate >= @StartDate");
        if (filter.EndDate.HasValue)
            conditions.Add("p.CDate <= @EndDate");
        return conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
    }
}
