using Dapper;
using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Models;
using TrackerPlus.Repository.Infrastructure;

namespace TrackerPlus.Repository.Repositories;

public class WebServiceIPRepository : IWebServiceIPRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<WebServiceIPRepository> _logger;

    public WebServiceIPRepository(IDbConnectionFactory db, ILogger<WebServiceIPRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<WebServiceIP>> GetAllAsync()
    {
        const string sql = "SELECT * FROM WebServiceIP WHERE Status='Y' ORDER BY TbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.QueryAsync<WebServiceIP>(sql);
    }

    public async Task<PagedResult<WebServiceIP>> GetPagedAsync(QueryFilter filter)
    {
        var offset = (filter.PageIndex - 1) * filter.PageSize;
        var where = BuildWhereClause(filter);

        var countSql = $"SELECT COUNT(*) FROM WebServiceIP {where}";
        var dataSql = $@"SELECT * FROM WebServiceIP {where}
            ORDER BY TbKey DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var param = new { filter.Keyword, Offset = offset, filter.PageSize };
        using var conn = _db.CreateMainConnection();
        var total = await conn.ExecuteScalarAsync<int>(countSql, param);
        var items = await conn.QueryAsync<WebServiceIP>(dataSql, param);

        return new PagedResult<WebServiceIP>
        {
            Items = items,
            TotalCount = total,
            PageIndex = filter.PageIndex,
            PageSize = filter.PageSize
        };
    }

    public async Task<WebServiceIP?> GetByIdAsync(int tbKey)
    {
        const string sql = "SELECT * FROM WebServiceIP WHERE TbKey=@TbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.QuerySingleOrDefaultAsync<WebServiceIP>(sql, new { TbKey = tbKey });
    }

    public async Task<int> CreateAsync(WebServiceIP ip)
    {
        _logger.LogInformation("新增 WebService IP {IP}", ip.IPAddress);
        const string sql = @"INSERT INTO WebServiceIP (IPAddress, Description, Status, CDate)
            VALUES (@IPAddress, @Description, @Status, @CDate);
            SELECT SCOPE_IDENTITY();";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteScalarAsync<int>(sql, ip);
    }

    public async Task<bool> DeleteAsync(int tbKey)
    {
        _logger.LogWarning("刪除 WebService IP TbKey={TbKey}", tbKey);
        const string sql = "DELETE FROM WebServiceIP WHERE TbKey=@TbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, new { TbKey = tbKey }) > 0;
    }

    public async Task<bool> IsAllowedAsync(string ipAddress)
    {
        const string sql = "SELECT COUNT(1) FROM WebServiceIP WHERE IPAddress=@IP AND Status='Y'";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { IP = ipAddress }) > 0;
    }

    private static string BuildWhereClause(QueryFilter filter)
    {
        var conditions = new List<string>();
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
            conditions.Add("(IPAddress LIKE '%'+@Keyword+'%' OR Description LIKE '%'+@Keyword+'%')");
        return conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
    }
}
