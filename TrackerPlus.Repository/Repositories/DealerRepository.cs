using Dapper;
using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Models;
using TrackerPlus.Repository.Infrastructure;

namespace TrackerPlus.Repository.Repositories;

public class DealerRepository : IDealerRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<DealerRepository> _logger;

    public DealerRepository(IDbConnectionFactory db, ILogger<DealerRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<Dealer>> GetAllAsync(string? keyword, string? status)
    {
        var conditions = new List<string>();
        if (!string.IsNullOrWhiteSpace(keyword))
            conditions.Add("(u.UserCode LIKE '%'+@Keyword+'%' OR u.UserName LIKE '%'+@Keyword+'%' OR u.UserID LIKE '%'+@Keyword+'%')");
        if (!string.IsNullOrWhiteSpace(status))
            conditions.Add("u.Status = @Status");

        var where = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
        var inputDate = DateTime.Today.AddDays(-1).ToString("yyyyMMdd");

        var sql = $@"
            SELECT
                u.tbKey, u.UserCode, u.UserName, u.UserID, u.UserPW,
                u.CreateDate, u.Status, ISNULL(u.PaymentService, 0) AS PaymentService,
                ISNULL(u.UserMemo,'') AS UserMemo, ISNULL(u.Tel,'') AS Tel,
                ISNULL(u.Email,'') AS Email, ISNULL(u.Contact,'') AS Contact,
                ISNULL(u.Webtitle,'') AS Webtitle, ISNULL(u.ServiceEmail,'') AS ServiceEmail,
                ISNULL(u.ServiceSenderTitle,'') AS ServiceSenderTitle,
                (SELECT COUNT(*) FROM dbo.Tracker t WHERE t.OBM_tbKey = u.tbKey) AS TotalDevices,
                (SELECT COUNT(*) FROM dbo.Tracker t WHERE t.OBM_tbKey = u.tbKey AND t.CurrentStatus = 'Y') AS OnlineDevices,
                ISNULL(d.TodayDev_Num, 0)       AS YesterdayDevices,
                ISNULL(d.TodayDev_LoginNum, 0)  AS YesterdayLogins,
                ISNULL(d.Dev_unSa90D, 0)        AS Offline90Days,
                ISNULL(d.Dev_expired, 0)        AS ExpiredDevices,
                ISNULL(d.Phone_Num, 0)          AS BoundPhones,
                ISNULL(d.Email_Num, 0)          AS BoundEmails
            FROM dbo.Userdb u
            LEFT JOIN dbo.OBM_Day d ON u.tbKey = d.Userdb_tbkey AND d.INPUT_DATE = @InputDate
            {where}
            ORDER BY u.UserCode";

        using var conn = _db.CreateMainConnection();
        return await conn.QueryAsync<Dealer>(sql, new { Keyword = keyword, Status = status, InputDate = inputDate });
    }

    public async Task<Dealer?> GetByIdAsync(int tbKey)
    {
        const string sql = @"
            SELECT
                tbKey, ISNULL(UserCode,'') AS UserCode, ISNULL(UserName,'') AS UserName,
                ISNULL(UserID,'') AS UserID, ISNULL(UserPW,'') AS UserPW,
                CreateDate, ISNULL(Status,'Y') AS Status,
                ISNULL(PaymentService, 0) AS PaymentService,
                ISNULL(UserMemo,'') AS UserMemo, ISNULL(Tel,'') AS Tel,
                ISNULL(Email,'') AS Email, ISNULL(Contact,'') AS Contact,
                ISNULL(Webtitle,'') AS Webtitle, ISNULL(ServiceEmail,'') AS ServiceEmail,
                ISNULL(ServiceSenderTitle,'') AS ServiceSenderTitle
            FROM dbo.Userdb
            WHERE tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.QuerySingleOrDefaultAsync<Dealer>(sql, new { TbKey = tbKey });
    }

    public async Task<bool> UpdateAsync(Dealer dealer)
    {
        _logger.LogInformation("更新經銷商 TbKey={TbKey}", dealer.TbKey);
        const string sql = @"
            UPDATE dbo.Userdb SET
                UserCode            = @UserCode,
                UserName            = @UserName,
                UserID              = @UserID,
                UserPW              = @UserPW,
                Status              = @Status,
                UserMemo            = @UserMemo,
                Tel                 = @Tel,
                Email               = @Email,
                Contact             = @Contact,
                Webtitle            = @Webtitle,
                ServiceEmail        = @ServiceEmail,
                ServiceSenderTitle  = @ServiceSenderTitle
            WHERE tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, dealer) > 0;
    }

    public async Task<int> CreateAsync(Dealer dealer)
    {
        _logger.LogInformation("新增經銷商 UserCode={UserCode}", dealer.UserCode);
        const string sql = @"
            INSERT INTO dbo.Userdb
                (UserCode, UserName, UserID, UserPW, Status, UserMemo, Tel, Email,
                 Contact, Webtitle, ServiceEmail, ServiceSenderTitle, CreateDate, PaymentService)
            VALUES
                (@UserCode, @UserName, @UserID, @UserPW, @Status, @UserMemo, @Tel, @Email,
                 @Contact, @Webtitle, @ServiceEmail, @ServiceSenderTitle, @CreateDate, 0);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            dealer.UserCode, dealer.UserName, dealer.UserID, dealer.UserPW,
            dealer.Status, dealer.UserMemo, dealer.Tel, dealer.Email,
            dealer.Contact, dealer.Webtitle, dealer.ServiceEmail, dealer.ServiceSenderTitle,
            CreateDate = DateTime.Now
        });
    }

    public async Task<bool> DeleteAsync(int tbKey)
    {
        _logger.LogInformation("刪除經銷商 TbKey={TbKey}", tbKey);
        const string sql = "DELETE FROM dbo.Userdb WHERE tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, new { TbKey = tbKey }) > 0;
    }
}
