using Dapper;
using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Models;
using TrackerPlus.Repository.Infrastructure;

namespace TrackerPlus.Repository.Repositories;

public class OBMRepository : IOBMRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<OBMRepository> _logger;

    public OBMRepository(IDbConnectionFactory db, ILogger<OBMRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<OBM?> GetByIdAsync(int tbKey)
    {
        const string sql = "SELECT * FROM OBM WHERE TbKey=@TbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.QuerySingleOrDefaultAsync<OBM>(sql, new { TbKey = tbKey });
    }

    public async Task<OBM?> GetByDomainAsync(string domain)
    {
        const string sql = "SELECT * FROM OBM WHERE Domain=@Domain AND Status='Y'";
        using var conn = _db.CreateMainConnection();
        return await conn.QuerySingleOrDefaultAsync<OBM>(sql, new { Domain = domain });
    }

    public async Task<IEnumerable<OBM>> GetAllAsync()
    {
        const string sql = "SELECT * FROM OBM WHERE Status='Y' ORDER BY CName";
        using var conn = _db.CreateMainConnection();
        return await conn.QueryAsync<OBM>(sql);
    }

    public async Task<PagedResult<OBM>> GetPagedAsync(QueryFilter filter)
    {
        var offset = (filter.PageIndex - 1) * filter.PageSize;
        var where = BuildWhereClause(filter);

        var countSql = $"SELECT COUNT(*) FROM OBM {where}";
        var dataSql = $@"SELECT * FROM OBM {where}
            ORDER BY TbKey DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var param = new { filter.Keyword, filter.Status, Offset = offset, filter.PageSize };
        using var conn = _db.CreateMainConnection();
        var total = await conn.ExecuteScalarAsync<int>(countSql, param);
        var items = await conn.QueryAsync<OBM>(dataSql, param);

        return new PagedResult<OBM>
        {
            Items = items,
            TotalCount = total,
            PageIndex = filter.PageIndex,
            PageSize = filter.PageSize
        };
    }

    public async Task<int> CreateAsync(OBM obm)
    {
        _logger.LogInformation("新增 OBM {CName}", obm.CName);
        const string sql = @"INSERT INTO OBM
            (CName, ContactPerson, Tel, Email, Addr, Status, CDate, CommissionRate, Memo, Domain, WebTitle, Logo)
            VALUES
            (@CName, @ContactPerson, @Tel, @Email, @Addr, @Status, @CDate, @CommissionRate, @Memo, @Domain, @WebTitle, @Logo);
            SELECT SCOPE_IDENTITY();";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteScalarAsync<int>(sql, obm);
    }

    public async Task<bool> UpdateAsync(OBM obm)
    {
        _logger.LogInformation("更新 OBM TbKey={TbKey}", obm.TbKey);
        const string sql = @"UPDATE OBM SET
            CName=@CName, ContactPerson=@ContactPerson, Tel=@Tel, Email=@Email,
            Addr=@Addr, Status=@Status, CommissionRate=@CommissionRate,
            Memo=@Memo, Domain=@Domain, WebTitle=@WebTitle, Logo=@Logo
            WHERE TbKey=@TbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, obm) > 0;
    }

    public async Task<bool> DeleteAsync(int tbKey)
    {
        _logger.LogWarning("刪除 OBM TbKey={TbKey}", tbKey);
        const string sql = "DELETE FROM OBM WHERE TbKey=@TbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, new { TbKey = tbKey }) > 0;
    }

    private static string BuildWhereClause(QueryFilter filter)
    {
        var conditions = new List<string>();
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
            conditions.Add("(CName LIKE '%'+@Keyword+'%' OR Domain LIKE '%'+@Keyword+'%')");
        if (!string.IsNullOrWhiteSpace(filter.Status))
            conditions.Add("Status=@Status");
        return conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
    }
}
