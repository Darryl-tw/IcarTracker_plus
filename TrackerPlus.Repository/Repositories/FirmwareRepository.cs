using Dapper;
using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Models;
using TrackerPlus.Repository.Infrastructure;

namespace TrackerPlus.Repository.Repositories;

public class FirmwareRepository : IFirmwareRepository
{
    private const string SelectColumns = @"
        RTRIM(FWVERSION) AS FWVERSION,
        RTRIM(FTP_SERVER) AS FtpServer,
        RTRIM(FTP_USERNAME) AS FtpUsername,
        RTRIM(FTP_PASSWORD) AS FtpPassword,
        RTRIM(FTP_DIR) AS FtpDir,
        RTRIM(fileName) AS FileName,
        RTRIM(FILESize) AS FileSize,
        RTRIM(NEW_FWVERSION) AS NewFwVersion,
        CDate";

    private readonly IDbConnectionFactory _db;
    private readonly ILogger<FirmwareRepository> _logger;

    public FirmwareRepository(IDbConnectionFactory db, ILogger<FirmwareRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<FirmwareVersion?> GetByVersionAsync(string fwVersion)
    {
        var sql = $"SELECT {SelectColumns} FROM dbo.FWCONROL WHERE RTRIM(FWVERSION) = @FWVERSION";
        using var conn = _db.CreateMainConnection();
        return await conn.QuerySingleOrDefaultAsync<FirmwareVersion>(sql, new { FWVERSION = fwVersion.Trim() });
    }

    public async Task<IEnumerable<FirmwareVersion>> GetAllAsync()
    {
        var sql = $"SELECT {SelectColumns} FROM dbo.FWCONROL ORDER BY CDate DESC";
        using var conn = _db.CreateMainConnection();
        return await conn.QueryAsync<FirmwareVersion>(sql);
    }

    public async Task<PagedResult<FirmwareVersion>> GetPagedAsync(QueryFilter filter)
    {
        var offset = (filter.PageIndex - 1) * filter.PageSize;
        var where = string.IsNullOrWhiteSpace(filter.Keyword)
            ? ""
            : "WHERE FWVERSION LIKE '%' + @Keyword + '%' OR NEW_FWVERSION LIKE '%' + @Keyword + '%' OR fileName LIKE '%' + @Keyword + '%'";

        var countSql = $"SELECT COUNT(*) FROM dbo.FWCONROL {where}";
        var dataSql = $@"SELECT {SelectColumns} FROM dbo.FWCONROL {where}
            ORDER BY CDate DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var param = new { filter.Keyword, Offset = offset, filter.PageSize };
        using var conn = _db.CreateMainConnection();
        var total = await conn.ExecuteScalarAsync<int>(countSql, param);
        var items = await conn.QueryAsync<FirmwareVersion>(dataSql, param);

        return new PagedResult<FirmwareVersion>
        {
            Items = items,
            TotalCount = total,
            PageIndex = filter.PageIndex,
            PageSize = filter.PageSize
        };
    }

    public async Task<bool> CreateAsync(FirmwareVersion firmware)
    {
        _logger.LogInformation("新增韌體 {Version}", firmware.FWVERSION);
        const string sql = @"INSERT INTO dbo.FWCONROL
            (FWVERSION, FTP_SERVER, FTP_USERNAME, FTP_PASSWORD, FTP_DIR, fileName, FILESize, NEW_FWVERSION, CDate)
            VALUES (@FWVERSION, @FtpServer, @FtpUsername, @FtpPassword, @FtpDir, @FileName, @FileSize, @NewFwVersion, GETUTCDATE())";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, firmware) > 0;
    }

    public async Task<bool> UpdateAsync(FirmwareVersion firmware, string originalFwVersion)
    {
        const string sql = @"UPDATE dbo.FWCONROL SET
            FWVERSION = @FWVERSION,
            FTP_SERVER = @FtpServer,
            FTP_USERNAME = @FtpUsername,
            FTP_PASSWORD = @FtpPassword,
            FTP_DIR = @FtpDir,
            fileName = @FileName,
            FILESize = @FileSize,
            NEW_FWVERSION = @NewFwVersion
            WHERE RTRIM(FWVERSION) = @Original";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, new
        {
            firmware.FWVERSION,
            firmware.FtpServer,
            firmware.FtpUsername,
            firmware.FtpPassword,
            firmware.FtpDir,
            firmware.FileName,
            firmware.FileSize,
            firmware.NewFwVersion,
            Original = originalFwVersion.Trim()
        }) > 0;
    }

    public async Task<bool> DeleteAsync(string fwVersion)
    {
        _logger.LogWarning("刪除韌體 {Version}", fwVersion);
        const string sql = "DELETE FROM dbo.FWCONROL WHERE RTRIM(FWVERSION) = @FWVERSION";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, new { FWVERSION = fwVersion.Trim() }) > 0;
    }

    public async Task<bool> QueueFirmwareUpdateAsync(string targetFwVersion, IEnumerable<string> imeiList)
    {
        var list = imeiList.Select(i => i.Trim()).Where(i => i.Length > 0).Distinct().ToList();
        if (list.Count == 0) return false;

        const string sql = @"
            UPDATE dbo.Tracker SET update_FWVERSION = @Target, isUpdate = 1
            WHERE RTRIM(IMEICode) IN @IMEIList";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, new { Target = targetFwVersion.Trim(), IMEIList = list }) > 0;
    }
}
