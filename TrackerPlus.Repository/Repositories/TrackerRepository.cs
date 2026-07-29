using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Models;
using TrackerPlus.Repository.Infrastructure;
using TrackerPlus.Repository.Mapping;

namespace TrackerPlus.Repository.Repositories;

public class TrackerRepository : ITrackerRepository
{
    private const string TrackerMemberJoins = @"
        LEFT JOIN dbo.Member m ON t.Member_tbKey = m.tbKey
        LEFT JOIN dbo.Userdb u ON t.OBM_tbKey = u.tbKey";

    private readonly IDbConnectionFactory _db;
    private readonly ILogger<TrackerRepository> _logger;

    public TrackerRepository(IDbConnectionFactory db, ILogger<TrackerRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Tracker?> GetByIdAsync(int tbKey)
    {
        _logger.LogDebug("取得追蹤器 TbKey={TbKey}", tbKey);
        var sql = $"{TrackerMapper.SelectColumns} {TrackerMapper.FromClause} WHERE t.tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        var row = await conn.QuerySingleOrDefaultAsync<TrackerDbRow>(sql, new { TbKey = tbKey });
        return row == null ? null : TrackerMapper.ToModel(row);
    }

    public async Task<Tracker?> GetByIMEIAsync(string imei)
    {
        _logger.LogDebug("取得追蹤器 IMEI={IMEI}", imei);
        var sql = $"{TrackerMapper.SelectColumns} {TrackerMapper.FromClause} WHERE RTRIM(t.IMEICode) = @IMEI";
        using var conn = _db.CreateMainConnection();
        var row = await conn.QuerySingleOrDefaultAsync<TrackerDbRow>(sql, new { IMEI = imei.Trim() });
        return row == null ? null : TrackerMapper.ToModel(row);
    }

    public async Task<IEnumerable<Tracker>> GetByMemberAsync(int memberTbKey)
    {
        _logger.LogDebug("取得會員追蹤器列表 Member={Member}", memberTbKey);
        var sql = $"{TrackerMapper.SelectColumns} {TrackerMapper.FromClause} WHERE t.Member_tbKey = @MemberTbKey ORDER BY RTRIM(ISNULL(t.CName,''))";
        using var conn = _db.CreateMainConnection();
        var rows = await conn.QueryAsync<TrackerDbRow>(sql, new { MemberTbKey = memberTbKey });
        return rows.Select(TrackerMapper.ToModel);
    }

    public async Task<PagedResult<Tracker>> GetPagedAsync(QueryFilter filter, int? memberTbKey = null)
    {
        var offset = (filter.PageIndex - 1) * filter.PageSize;
        var where = BuildWhereClause(filter, memberTbKey);

        var orderBy = BuildOrderByClause(filter);

        var countSql = $"SELECT COUNT(*) FROM dbo.Tracker t {TrackerMemberJoins} {where}";

        // 先用 CTE 取出分頁的 tbKey（不含昂貴的 PayLog 子查詢），
        // 再 INNER JOIN 回來只對這 PageSize 筆執行完整欄位查詢，避免全表掃描效能問題。
        var dataSql = $@"
WITH _PageKeys AS (
    SELECT t.tbKey
    FROM dbo.Tracker t
    {TrackerMemberJoins}
    {where}
    {orderBy}
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
){TrackerMapper.SelectColumns},
            RTRIM(ISNULL(m.CName,'')) AS MemberName,
            RTRIM(ISNULL(u.UserName,'')) AS OBMName,
            RTRIM(ISNULL(m.ID,'')) AS MemberAccount,
            ISNULL((SELECT COUNT(*) FROM dbo.GCM_EMAIL g WHERE g.EMAIL = m.ID), 0)
              + ISNULL((SELECT COUNT(*) FROM dbo.ClientIDRegistration c WHERE c.ID_tbkey = m.tbKey), 0) AS BindCount
{TrackerMapper.FromClause}
INNER JOIN _PageKeys pk ON t.tbKey = pk.tbKey
{TrackerMemberJoins}
{orderBy}";

        var param = new
        {
            filter.Obm,
            filter.Imei,
            filter.Account,
            filter.Keyword,
            Status = filter.Status,
            MemberTbKey = memberTbKey,
            Offset = offset,
            filter.PageSize
        };
        using var conn = _db.CreateMainConnection();
        var total = await conn.ExecuteScalarAsync<int>(countSql, param);
        var rows = await conn.QueryAsync<TrackerDbRow>(dataSql, param, commandTimeout: 60);

        return new PagedResult<Tracker>
        {
            Items = rows.Select(TrackerMapper.ToModel),
            TotalCount = total,
            PageIndex = filter.PageIndex,
            PageSize = filter.PageSize
        };
    }

    public async Task<int> CreateAsync(Tracker tracker)
    {
        _logger.LogInformation("新增追蹤器 IMEI={IMEI}", tracker.IMEICODE);
        const string sql = @"INSERT INTO dbo.Tracker
            (IMEICode, Member_tbKey, CName, TrackerEnabled, PowerSavingMode, SOSTel1, CMemo, CDate, TrackerEnabledDate)
            VALUES
            (@IMEICode, @Member_TbKey, @CName, @TrackerEnabled, @PowerSavingMode, @SOSTel1, @CMemo, GETUTCDATE(), GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() AS int);";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            IMEICode = tracker.IMEICODE,
            tracker.Member_TbKey,
            tracker.CName,
            TrackerEnabled = tracker.TrackerStatus,
            tracker.PowerSavingMode,
            SOSTel1 = tracker.SosNumber,
            CMemo = tracker.Memo
        });
    }

    public async Task<bool> UpdateAsync(Tracker tracker)
    {
        _logger.LogInformation("更新追蹤器 TbKey={TbKey}", tracker.TbKey);
        const string sql = @"UPDATE dbo.Tracker SET
            CName = @CName,
            TrackerEnabled = @TrackerEnabled,
            PowerSavingMode = @PowerSavingMode,
            SOSTel1 = @SOSTel1,
            CMemo = @CMemo
            WHERE tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        var rows = await conn.ExecuteAsync(sql, new
        {
            tracker.TbKey,
            tracker.CName,
            TrackerEnabled = tracker.TrackerStatus,
            tracker.PowerSavingMode,
            SOSTel1 = tracker.SosNumber,
            CMemo = tracker.Memo
        });
        return rows > 0;
    }

    public async Task<bool> UpdateLiveSettingsAsync(int tbKey, string cname, string memo, string iconFile)
    {
        const string sql = @"UPDATE dbo.Tracker SET
            CName = @CName,
            CMemo = @CMemo,
            IconFile = @IconFile
            WHERE tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        var rows = await conn.ExecuteAsync(sql, new
        {
            TbKey = tbKey,
            CName = cname.Trim(),
            CMemo = memo?.Trim() ?? string.Empty,
            IconFile = iconFile.Trim().ToUpperInvariant()
        });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int tbKey)
    {
        _logger.LogWarning("刪除追蹤器 TbKey={TbKey}", tbKey);
        const string sql = "DELETE FROM dbo.Tracker WHERE tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        var rows = await conn.ExecuteAsync(sql, new { TbKey = tbKey });
        return rows > 0;
    }

    public async Task<bool> UpdateStatusAsync(int tbKey, string status)
    {
        const string sql = "UPDATE dbo.Tracker SET TrackerEnabled = @Status WHERE tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        var rows = await conn.ExecuteAsync(sql, new { TbKey = tbKey, Status = status });
        return rows > 0;
    }

    public async Task<bool> UpdateGeofenceAsync(int tbKey, Geofence geofence)
    {
        _logger.LogInformation("更新電子柵欄 TbKey={TbKey} Index={Index}", tbKey, geofence.FenceIndex);
        var idx = geofence.FenceIndex;
        var suffix = idx == 0 ? string.Empty : idx.ToString();
        var sql = $@"UPDATE dbo.Tracker SET
            FC_Lat{suffix} = @Lat,
            FC_Lng{suffix} = @Lng,
            FC_R{suffix} = @Radius,
            isFCEnable{suffix} = @Enable,
            initParameter = 'Y'
            WHERE tbKey = @TbKey";
        using var conn = _db.CreateMainConnection();
        var rows = await conn.ExecuteAsync(sql, new
        {
            TbKey = tbKey,
            Lat = geofence.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Lng = geofence.Lng.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Radius = ((int)geofence.Radius).ToString(),
            Enable = geofence.Enable is "Y" or "1" ? "1" : "0"
        });
        return rows > 0;
    }

    public async Task<bool> UpdateLiveLocationAsync(string imei, double lat, double lng, double speed, int direction, DateTime utcTime)
    {
        // 即時位置由裝置寫入 Tracker_Info / TLG，Web 端僅讀取
        _logger.LogDebug("略過 UpdateLiveLocationAsync（由裝置回報） IMEI={IMEI}", imei);
        return await Task.FromResult(true);
    }

    public async Task<IEnumerable<Tracker>> GetByGroupAsync(int memberTbKey, string groupName)
    {
        // 舊版群組在 UDLabel，雛型階段回傳該會員全部啟用裝置
        return await GetByMemberAsync(memberTbKey);
    }

    public async Task<int> GetCountAsync(int? memberTbKey = null)
    {
        var sql = memberTbKey.HasValue
            ? "SELECT COUNT(*) FROM dbo.Tracker WHERE Member_tbKey = @MemberTbKey"
            : "SELECT COUNT(*) FROM dbo.Tracker";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { MemberTbKey = memberTbKey });
    }

    public async Task<OperationResult> BatchMoveDevicesAsync(BatchMoveDevicesRequest req)
    {
        var list = req.Imeis
            .Select(x => x.Trim())
            .Where(x => x.Length == 15 && x.All(char.IsDigit))
            .Distinct()
            .ToList();
        if (list.Count == 0) return OperationResult.Fail("無 IMEI");

        var dealerLabel = string.IsNullOrWhiteSpace(req.TargetDealerLabel)
            ? req.OBMTbKey.ToString()
            : req.TargetDealerLabel.Trim();
        var iconFile = NormalizeIconFile(req.IconFile);
        var subAdminKey = req.SubAdminUserTbKey;
        var errors = new List<string>();
        var success = 0;
        var now = DateTime.Now;

        using var conn = _db.CreateMainConnection();

        foreach (var imei in list)
        {
            try
            {
                await conn.ExecuteAsync(
                    "DELETE FROM dbo.SendGCM WHERE RTRIM(IMEICODE)=@IMEI",
                    new { IMEI = imei });

                var info = await conn.QueryFirstOrDefaultAsync<ImeiMoveLookupRow>(
                    @"SELECT A.Tracker_tbKey AS TrackerTbKey, A.OBM_tbKey AS ObmTbKey, B.Member_tbKey AS MemberTbKey
                      FROM dbo.IMEITable A
                      LEFT JOIN dbo.Tracker B ON A.Tracker_tbKey = B.tbKey
                      WHERE RTRIM(A.IMEICODE)=@IMEI",
                    new { IMEI = imei });

                if (info == null || info.TrackerTbKey <= 0)
                {
                    errors.Add($"查無此:{imei}，搬移失敗");
                    continue;
                }

                var moveMemo = $"此定位器於{now:yyyy/MM/dd HH:mm:ss}搬移至{dealerLabel}";
                var movOrder = $"MOV_{now:yyyyMMdd}";

                await conn.ExecuteAsync(
                    @"INSERT INTO dbo.PayLog
                      (Member_tbKey, Tracker_tbKey, IMEICode, OrderNumber, Amount, SDate, EDate, CMemo, ValueAddedFunc, Sub_adminUser_tbkey)
                      VALUES (@MemberTbKey, @TrackerTbKey, @IMEI, @OrderNumber, 0, @Now, @Now, @CMemo, 'N', @SubAdminKey)",
                    new
                    {
                        MemberTbKey = info.MemberTbKey,
                        TrackerTbKey = info.TrackerTbKey,
                        IMEI = imei,
                        OrderNumber = movOrder,
                        Now = now,
                        CMemo = moveMemo,
                        SubAdminKey = subAdminKey
                    });

                if (req.DefaultPay)
                {
                    var (sDate, eDate, orderNo, amount) = ComputeDefaultPayLog(req);
                    var skipInsert = false;
                    if (req.SaleModel is 4 or 5 or 7)
                    {
                        var existing = await conn.ExecuteScalarAsync<int>(
                            @"SELECT COUNT(*) FROM dbo.PayLog
                              WHERE Member_tbKey=@MemberTbKey AND RTRIM(IMEICode)=@IMEI
                                AND Model IN (4,5,7)
                                AND CONVERT(char(8), SDate, 112)='19000101'
                                AND CONVERT(char(8), EDate, 112)='19000101'",
                            new { MemberTbKey = info.MemberTbKey, IMEI = imei });
                        skipInsert = existing > 0;
                    }

                    if (!skipInsert)
                    {
                        await conn.ExecuteAsync(
                            @"INSERT INTO dbo.PayLog
                              (Member_tbKey, Tracker_tbKey, IMEICode, OrderNumber, Amount, SDate, EDate, CMemo, ValueAddedFunc,
                               Sub_adminUser_tbkey, Model, ValueAddedWeb, sale_memo)
                              VALUES (@MemberTbKey, @TrackerTbKey, @IMEI, @OrderNo, @Amount, @SDate, @EDate, N'轉移經銷商的預設訂單', 'N',
                                      @SubAdminKey, @Model, @ValueAddedWeb, @SaleMemo)",
                            new
                            {
                                MemberTbKey = info.MemberTbKey,
                                TrackerTbKey = info.TrackerTbKey,
                                IMEI = imei,
                                OrderNo = orderNo,
                                Amount = amount,
                                SDate = sDate,
                                EDate = eDate,
                                SubAdminKey = subAdminKey,
                                Model = req.SaleModel,
                                ValueAddedWeb = req.ValueAddedWeb,
                                SaleMemo = req.SaleMemo?.Trim() ?? string.Empty
                            });
                    }
                }

                await conn.ExecuteAsync(
                    @"UPDATE dbo.IMEITable SET OBM_tbKey=@OBM WHERE RTRIM(IMEICODE)=@IMEI;
                      UPDATE dbo.Tracker SET OBM_tbKey=@OBM WHERE RTRIM(IMEICode)=@IMEI",
                    new { OBM = req.OBMTbKey, IMEI = imei });

                if (req.ResetOnlineTime)
                {
                    await conn.ExecuteAsync(
                        @"UPDATE dbo.Tracker SET
                            LastConnectedDate='2001-01-01', LastReportDate=NULL,
                            Lastlogintime='2001-01-01', FirstLoginDateTime=NULL,
                            OBM_tbKey=@OBM, IconFile=@Icon, IsSimBundled=@Sim
                          WHERE RTRIM(IMEICode)=@IMEI",
                        new { OBM = req.OBMTbKey, IMEI = imei, Icon = iconFile, Sim = req.IsSimBundled });
                }
                else
                {
                    await conn.ExecuteAsync(
                        @"UPDATE dbo.Tracker SET IconFile=@Icon, IsSimBundled=@Sim WHERE RTRIM(IMEICode)=@IMEI",
                        new { IMEI = imei, Icon = iconFile, Sim = req.IsSimBundled });
                }

                await conn.ExecuteAsync(
                    @"INSERT INTO dbo.Sub_adminFuntionLog (Sub_adminUser_tbkey, IMEICODE, Admin_Funtion, Memo)
                      VALUES (@SubAdminKey, @IMEI, N'搬移經銷商', @Memo)",
                    new { SubAdminKey = subAdminKey, IMEI = imei, Memo = moveMemo });

                if (req.SaleModel != 6)
                {
                    try
                    {
                        await conn.ExecuteAsync(
                            "delothersystem570",
                            new { imeicode = imei },
                            commandType: CommandType.StoredProcedure);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "delothersystem570 失敗 IMEI={IMEI}", imei);
                    }
                }

                if (req.SaleModel != 1)
                {
                    var activeModel1 = await conn.ExecuteScalarAsync<int>(
                        @"SELECT COUNT(*) FROM dbo.PayLog
                          WHERE RTRIM(IMEICode)=@IMEI AND Model=1
                            AND SDate<=GETDATE() AND EDate>=GETDATE()-1 AND SDate<>EDate",
                        new { IMEI = imei });
                    if (activeModel1 > 0)
                    {
                        await conn.ExecuteAsync(
                            @"UPDATE dbo.PayLog SET EDate=GETDATE()-1
                              WHERE RTRIM(IMEICode)=@IMEI AND Model=1
                                AND SDate<=GETDATE() AND EDate>=GETDATE()-1 AND SDate<>EDate",
                            new { IMEI = imei });
                    }
                }

                success++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批次轉移失敗 IMEI={IMEI}", imei);
                errors.Add($"{imei} 發生錯誤，搬移失敗");
            }
        }

        var msg = $"已新增 {success} 筆裝置移至 {dealerLabel} 經銷商下";
        if (errors.Count > 0)
            msg += "\n" + string.Join("\n", errors);
        return success > 0 || errors.Count == 0
            ? OperationResult.Ok(msg, success)
            : OperationResult.Fail(msg);
    }

    private static (DateTime sDate, DateTime eDate, string orderNo, decimal amount) ComputeDefaultPayLog(BatchMoveDevicesRequest req)
    {
        var saleModel = req.SaleModel;
        var sDate = req.SDate?.Date ?? DateTime.Today;
        var eds = (req.EndDateStatus ?? "1").Trim();
        var fed = req.EDate?.Date;

        if (saleModel is 4 or 5 or 7 or 21)
            sDate = new DateTime(1900, 1, 1);

        DateTime eDate;
        if (eds == "S")
            eDate = new DateTime(2070, 1, 1);
        else if (eds == "A" && fed.HasValue)
            eDate = fed.Value;
        else if (eds == "B")
            eDate = sDate.AddMonths(req.FMonth > 0 ? req.FMonth : 1);
        else if (eds == "0")
            eDate = sDate.AddDays(7);
        else if (int.TryParse(eds, out var months))
            eDate = sDate.AddMonths(months);
        else
            eDate = sDate.AddMonths(1);

        if (saleModel is 4 or 5 or 7 or 21)
            eDate = new DateTime(1900, 1, 1);
        if (saleModel == 13 && fed.HasValue)
            eDate = fed.Value;

        var orderNo = saleModel == 6
            ? $"BAK_{DateTime.Now:yyyyMMdd}"
            : req.OrderNo.Trim();
        if (saleModel == 6)
            eDate = sDate;

        var amount = saleModel == 4 ? 999999m : req.Amount;
        return (sDate, eDate, orderNo, amount);
    }

    private static string NormalizeIconFile(string? icon)
    {
        if (string.IsNullOrWhiteSpace(icon)) return string.Empty;
        var c = char.ToUpperInvariant(icon.Trim()[0]);
        return c is >= 'A' and <= 'H' ? c.ToString() : string.Empty;
    }

    private sealed class ImeiMoveLookupRow
    {
        public int TrackerTbKey { get; set; }
        public int ObmTbKey { get; set; }
        public int MemberTbKey { get; set; }
    }

    /// <summary>對齊舊 IMEI_New.aspx.vb btnCheck_Click 流程。</summary>
    public async Task<OperationResult> InsertDevicesAsync(IEnumerable<string> imeis, int subAdminUserTbKey)
    {
        var lines = imeis
            .Select(x => (x ?? string.Empty).Trim())
            .Where(x => x.Length > 0)
            .ToList();
        if (lines.Count == 0)
            return OperationResult.Fail("NO_IMEI");

        const int memberTbKey = 0; // Administrator
        const int obmTbKey = 1;
        var sDate = DateTime.Today;
        var eDate = sDate.AddMonths(1);
        var orderNo = "init" + sDate.ToString("yyMMdd");
        var createMemo = "CREATE:" + sDate.ToString("yyyy-MM-dd HH:mm:ss");

        var errors = new List<string>();
        var success = 0;

        using var conn = _db.CreateMainConnection();
        await conn.OpenAsync();

        var defaults = await conn.QuerySingleOrDefaultAsync<SystemOptionsRow>(
            "SELECT TOP 1 TimeZone_tbKey, GPSReportTime, SMSPassword, SOSCT FROM dbo.Options");
        var timeZone = defaults?.TimeZone_tbKey ?? 29;
        var gpsReportTime = defaults?.GPSReportTime ?? 60;
        var smsPassword = defaults?.SMSPassword?.Trim() ?? "0000";
        var sosCt = defaults?.SOSCT?.Trim() ?? "+886";

        foreach (var raw in lines)
        {
            var imei = raw;
            try
            {
                if (imei.Length != 15 || !imei.All(char.IsDigit))
                {
                    errors.Add($"IMEI_FORMAT:{imei}");
                    continue;
                }

                var existingTracker = await conn.ExecuteScalarAsync<int?>(
                    "SELECT TOP 1 tbKey FROM dbo.Tracker WHERE RTRIM(IMEICode)=@IMEI",
                    new { IMEI = imei });
                if (existingTracker is > 0)
                {
                    errors.Add($"IMEI_EXISTS:{imei}");
                    continue;
                }

                var imeiCount = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM dbo.IMEITable WHERE RTRIM(IMEICODE)=@IMEI",
                    new { IMEI = imei });
                if (imeiCount == 0)
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO dbo.IMEITable (OBM_tbKey, IMEICODE, SERIALCODE, CDate, STATUS, CMemo)
                          VALUES (@OBM, @IMEI, @IMEI, GETUTCDATE(), 'Y', 'AUTO CREATE')",
                        new { OBM = obmTbKey, IMEI = imei });
                }

                const string insertTracker = @"
                    INSERT INTO dbo.Tracker (
                        TrackerEnabledDate, OBM_tbKey, UserLevel, Member_tbKey, IMEICode, CName,
                        TrackerEnabled, CurrentStatus, CDate, TimeZone_tbKey, GPSReportTime, IconFile,
                        SOSCT1, SOSTEL1, SOSCT2, SOSTEL2, SOSCT3, SOSTEL3, SOSMSG, SMSPassword, initParameter, FWVERSION)
                    VALUES (
                        GETUTCDATE(), @OBM, 'N', @Member, @IMEI, @IMEI, 'Y', 'N',
                        GETUTCDATE(),
                        @TZ, @GPS, 'A', @SOS, '', @SOS, '', @SOS, '', '', @SMS, 'N', '');
                    SELECT CAST(SCOPE_IDENTITY() AS int);";

                var trackerTbKey = await conn.ExecuteScalarAsync<int>(insertTracker, new
                {
                    OBM = obmTbKey,
                    Member = memberTbKey,
                    IMEI = imei,
                    TZ = timeZone,
                    GPS = gpsReportTime,
                    SOS = sosCt,
                    SMS = smsPassword
                });

                if (trackerTbKey <= 0)
                {
                    await conn.ExecuteAsync(
                        "DELETE FROM dbo.Tracker WHERE RTRIM(IMEICode)=@IMEI",
                        new { IMEI = imei });
                    errors.Add($"IMEI_CREATE_FAIL:{imei}");
                    continue;
                }

                await conn.ExecuteAsync(
                    @"UPDATE dbo.IMEITable SET STATUS='Y', Tracker_tbKey=@TrackerKey
                      WHERE OBM_tbKey=@OBM AND RTRIM(SERIALCODE)=@IMEI;
                      IF NOT EXISTS (SELECT 1 FROM dbo.Tracker_Info WHERE RTRIM(IMEICode)=@IMEI)
                          INSERT INTO dbo.Tracker_Info (IMEICode) VALUES (@IMEI);",
                    new { TrackerKey = trackerTbKey, OBM = obmTbKey, IMEI = imei });

                await EnsureAlertModuleDefaultAsync(conn, trackerTbKey, imei, memberTbKey);
                await EnsurePerImeiLogTablesAsync(imei);

                await conn.ExecuteAsync(
                    @"IF NOT EXISTS (
                          SELECT 1 FROM dbo.PayLog
                          WHERE RTRIM(IMEICode)=@IMEI AND SDate=@SDate AND EDate=@EDate)
                      INSERT INTO dbo.PayLog (
                          Member_tbKey, Tracker_tbKey, IMEICode, OrderNumber, Amount,
                          ValueAddedFunc, ValueAddedWeb, Model, Sub_adminUser_tbkey, SDate, EDate, CMemo)
                      VALUES (
                          @Member, @TrackerKey, @IMEI, @OrderNo, 0,
                          'Y', 1, 1, @SubKey, @SDate, @EDate, @CMemo)",
                    new
                    {
                        IMEI = imei,
                        SDate = sDate,
                        EDate = eDate,
                        Member = memberTbKey,
                        TrackerKey = trackerTbKey,
                        OrderNo = orderNo,
                        SubKey = subAdminUserTbKey,
                        CMemo = createMemo
                    });

                await conn.ExecuteAsync(
                    @"INSERT INTO dbo.Sub_adminFuntionLog (Sub_adminUser_tbkey, IMEICODE, Admin_Funtion, Memo)
                      VALUES (@SubKey, @IMEI, N'增加裝置', '')",
                    new { SubKey = subAdminUserTbKey, IMEI = imei });

                success++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "新增裝置失敗 IMEI={IMEI}", imei);
                errors.Add($"IMEI_EXCEPTION:{imei}|{ex.Message}");
            }
        }

        // Message 格式：成功筆數|錯誤碼列表（Controller 組裝在地化訊息）
        var payload = $"{success}|{string.Join("\n", errors)}";
        return OperationResult.Ok(payload, success);
    }

    private static async Task EnsureAlertModuleDefaultAsync(
        Microsoft.Data.SqlClient.SqlConnection conn, int trackerTbKey, string imei, int memberTbKey)
    {
        const string sql = @"
            IF NOT EXISTS (SELECT 1 FROM dbo.AlertModule WHERE Tracker_tbKey=@TrackerKey AND Member_tbKey=@Member)
            INSERT INTO dbo.AlertModule (
                Member_tbKey, Tracker_tbKey, ATEnabled, ATWeekDay, ATWeekTime,
                ACEnabled, ACMinute, APEnabled, APMinute, ASEnabled, ASMinute, ABEnabled,
                ATMsg, ACMsg, APMsg, ASMsg, ABMsg, AR_EMail1, AR_EMail2, AR_EMail3, AR_SMS1, AR_SMS2, AR_SMS3)
            VALUES (@Member, @TrackerKey, 'N', '', '00:00', 'N', 0, 'N', 0, 'N', 0, 'N',
                @Msg, @Msg, @Msg, @Msg, @Msg, '', '', '', '', '', '')";
        await conn.ExecuteAsync(sql, new { TrackerKey = trackerTbKey, Member = memberTbKey, Msg = imei });
    }

    private async Task EnsurePerImeiLogTablesAsync(string imei)
    {
        // IMEI 已驗證為 15 碼數字，可安全組成表名
        using var logConn = _db.CreateLogConnection();
        try
        {
            await logConn.OpenAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "無法連線 Log DB，略過建立 per-IMEI 表 IMEI={IMEI}", imei);
            return;
        }

        async Task EnsureTable(string tableName, string createSql)
        {
            try
            {
                var exists = await logConn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM sys.tables WHERE name=@Name AND schema_id=SCHEMA_ID('dbo')",
                    new { Name = tableName });
                if (exists == 0)
                    await logConn.ExecuteAsync(createSql);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "建立 Log 表失敗 {Table} IMEI={IMEI}", tableName, imei);
            }
        }

        var tl = "TL_" + imei;
        var tls = "TLS_" + imei;
        var alert = "AlertLog_" + imei;
        var sys = "SYSLog_" + imei;

        await EnsureTable(tl,
            $@"CREATE TABLE dbo.[{tl}] (
                [tbKey] [int] PRIMARY KEY IDENTITY (1, 1) NOT NULL,
                [CDate] [datetime] NOT NULL,
                [CmdStr] [nvarchar] (700) NOT NULL,
                [CMode] [char] (1) NOT NULL) ON [PRIMARY]");

        await EnsureTable(tls,
            $@"CREATE TABLE dbo.[{tls}] (
                [tbKey] [int] PRIMARY KEY IDENTITY (1, 1) NOT NULL,
                [SDate] [datetime] NOT NULL,
                [EDate] [datetime] NULL,
                [TimeZone_tbKey] [int] NOT NULL,
                [SDesc] [nchar] (20) NULL) ON [PRIMARY];
              CREATE INDEX [IDX_SD_{imei}] ON dbo.[{tls}] (SDate DESC)");

        await EnsureTable(alert,
            $@"CREATE TABLE dbo.[{alert}] (
                tbKey int PRIMARY KEY IDENTITY,
                CDate datetime NOT NULL,
                LogType char(2) NOT NULL,
                CMemo nvarchar(100),
                Lat char(10),
                Lng char(10))");

        await EnsureTable(sys,
            $@"CREATE TABLE dbo.[{sys}] (
                tbKey int PRIMARY KEY IDENTITY,
                CDate datetime NOT NULL,
                LogType char(2) NOT NULL,
                CMemo nvarchar(100),
                Lat char(10),
                Lng char(10))");
    }

    private sealed class SystemOptionsRow
    {
        public int TimeZone_tbKey { get; set; }
        public int GPSReportTime { get; set; }
        public string SMSPassword { get; set; } = "0000";
        public string SOSCT { get; set; } = "+886";
    }

    public async Task<OperationResult> FactoryResetAsync(int tbKey)
    {
        _logger.LogWarning("恢復出廠 TbKey={TbKey}", tbKey);
        using var conn = _db.CreateMainConnection();
        await conn.ExecuteAsync("DELETE FROM dbo.AlertModule WHERE Tracker_tbKey=@TbKey", new { TbKey = tbKey });
        await conn.ExecuteAsync("DELETE FROM dbo.UDFieldsValue WHERE Tracker_tbKey=@TbKey", new { TbKey = tbKey });
        await conn.ExecuteAsync("DELETE FROM dbo.UDLabelValue WHERE Tracker_tbKey=@TbKey", new { TbKey = tbKey });
        var rows = await conn.ExecuteAsync(
            "UPDATE dbo.Tracker SET Member_tbKey=0, CName='', Password='12345678' WHERE tbKey=@TbKey",
            new { TbKey = tbKey });
        return rows > 0 ? OperationResult.Ok("恢復出廠成功") : OperationResult.Fail("找不到裝置");
    }

    public async Task<int> BatchDeleteByKeysAsync(IEnumerable<int> tbKeys)
    {
        var list = tbKeys.ToList();
        if (list.Count == 0) return 0;
        using var conn = _db.CreateMainConnection();
        int deleted = 0;
        foreach (var key in list)
            deleted += await conn.ExecuteAsync("DELETE FROM dbo.Tracker WHERE tbKey=@TbKey", new { TbKey = key });
        return deleted;
    }

    public async Task<(bool Success, string? ErrorCode)> DeleteDeviceByImeiAsync(string imei, int subAdminUserTbKey, string? adminFunction = null)
    {
        imei = imei.Trim();
        if (imei.Length != 15 || !imei.All(char.IsDigit))
            return (false, "INVALID_IMEI");

        var fun = string.IsNullOrWhiteSpace(adminFunction) ? "Delete Drive" : adminFunction.Trim();

        using var conn = _db.CreateMainConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        try
        {
            var trackerTbKey = await conn.ExecuteScalarAsync<int?>(
                "SELECT tbKey FROM dbo.Tracker WHERE RTRIM(IMEICode)=@IMEI",
                new { IMEI = imei }, tx);
            if (trackerTbKey is null or <= 0)
            {
                tx.Rollback();
                return (false, "NOT_FOUND");
            }

            var histCount = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM dbo.DelHistoryTLGAll WHERE RTRIM(IMEICODE)=@IMEI",
                new { IMEI = imei }, tx);
            if (histCount == 0)
            {
                await conn.ExecuteAsync(
                    @"INSERT INTO dbo.DelHistoryTLGAll (IMEICODE, TrackerEnableDate)
                      SELECT RTRIM(IMEICode), TrackerEnabledDate FROM dbo.Tracker WHERE RTRIM(IMEICode)=@IMEI",
                    new { IMEI = imei }, tx);
            }
            else
            {
                await conn.ExecuteAsync(
                    "UPDATE dbo.DelHistoryTLGAll SET DelDateTime=GETUTCDATE() WHERE RTRIM(IMEICODE)=@IMEI",
                    new { IMEI = imei }, tx);
            }

            await conn.ExecuteAsync(
                @"DELETE FROM dbo.IMEITable WHERE RTRIM(IMEICODE)=@IMEI;
                  DELETE FROM dbo.AlertModule WHERE Tracker_tbKey=@TbKey;
                  DELETE FROM dbo.UDFieldsValue WHERE Tracker_tbKey=@TbKey;
                  DELETE FROM dbo.UDLabelValue WHERE Tracker_tbKey=@TbKey;
                  DELETE FROM dbo.Tracker WHERE RTRIM(IMEICode)=@IMEI;
                  DELETE FROM dbo.Tracker_Info WHERE RTRIM(IMEICode)=@IMEI;
                  DELETE FROM dbo.PayLog WHERE RTRIM(IMEICode)=@IMEI;
                  DELETE FROM dbo.ICARMEMBER WHERE RTRIM(IMEICODE)=@IMEI;
                  DELETE FROM dbo.SendGCM WHERE RTRIM(IMEICODE)=@IMEI;
                  DELETE FROM dbo.SendGCMLog WHERE RTRIM(IMEICODE)=@IMEI;",
                new { IMEI = imei, TbKey = trackerTbKey }, tx);

            await conn.ExecuteAsync(
                @"INSERT INTO dbo.Sub_adminFuntionLog (Sub_adminUser_tbkey, IMEICODE, Admin_Funtion, Memo)
                  VALUES (@SubKey, @IMEI, @Fun, '')",
                new { SubKey = subAdminUserTbKey, IMEI = imei, Fun = fun }, tx);

            tx.Commit();
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "刪除裝置失敗 IMEI={IMEI}", imei);
            return (false, ex.Message);
        }

        await DropPerImeiLogTablesSafeAsync(imei);
        return (true, null);
    }

    private async Task DropPerImeiLogTablesSafeAsync(string imei)
    {
        var suffixes = new[] { "AlertLog_", "SYSLog_", "TL_", "TLS_" };
        using var logConn = _db.CreateLogConnection();
        try
        {
            await logConn.OpenAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "無法連線 Log DB，略過 DROP per-IMEI 表 IMEI={IMEI}", imei);
            return;
        }

        foreach (var suffix in suffixes)
        {
            var tableName = suffix + imei;
            try
            {
                var exists = await logConn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM sys.tables WHERE name = @Name AND schema_id = SCHEMA_ID('dbo')",
                    new { Name = tableName });
                if (exists > 0)
                    await logConn.ExecuteAsync($"DROP TABLE dbo.[{tableName}]");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DROP Log 表失敗 {Table} IMEI={IMEI}", tableName, imei);
            }
        }
    }

    public async Task<OperationResult> UnbindAsync(int tbKey)
    {
        _logger.LogWarning("解除裝置綁定 TbKey={TbKey}", tbKey);
        using var conn = _db.CreateMainConnection();
        await conn.ExecuteAsync(
            "UPDATE dbo.AlertModule SET Member_tbKey=0 WHERE Tracker_tbKey=@TbKey",
            new { TbKey = tbKey });
        var rows = await conn.ExecuteAsync(
            "UPDATE dbo.Tracker SET Member_tbKey=0, Password='12345678' WHERE tbKey=@TbKey",
            new { TbKey = tbKey });
        return rows > 0 ? OperationResult.Ok("解除裝置成功") : OperationResult.Fail("找不到裝置");
    }

    public async Task<OperationResult> UnbindAllByMemberAsync(int memberTbKey)
    {
        _logger.LogWarning("解除會員所有裝置綁定 MemberTbKey={Key}", memberTbKey);
        using var conn = _db.CreateMainConnection();
        await conn.ExecuteAsync(
            "UPDATE dbo.AlertModule SET Member_tbKey=0 WHERE Tracker_tbKey IN (SELECT tbKey FROM dbo.Tracker WHERE Member_tbKey=@MemberTbKey)",
            new { MemberTbKey = memberTbKey });
        var rows = await conn.ExecuteAsync(
            "UPDATE dbo.Tracker SET Member_tbKey=0, Password='12345678' WHERE Member_tbKey=@MemberTbKey",
            new { MemberTbKey = memberTbKey });
        return rows > 0 ? OperationResult.Ok($"已解除 {rows} 台裝置") : OperationResult.Fail("找不到裝置或該會員無綁定裝置");
    }

    private static string BuildWhereClause(QueryFilter filter, int? memberTbKey)
    {
        var conditions = new List<string>();
        if (memberTbKey.HasValue)
            conditions.Add("t.Member_tbKey = @MemberTbKey");
        if (!string.IsNullOrWhiteSpace(filter.Obm))
            conditions.Add("RTRIM(u.UserName) LIKE '%' + @Obm + '%'");
        if (!string.IsNullOrWhiteSpace(filter.Imei))
            conditions.Add(@"(RTRIM(t.IMEICode) LIKE '%' + @Imei + '%'
                OR RTRIM(t.CName) LIKE '%' + @Imei + '%')");
        if (!string.IsNullOrWhiteSpace(filter.Account))
            conditions.Add(@"(RTRIM(m.ID) LIKE '%' + @Account + '%'
                OR RTRIM(m.EMail) LIKE '%' + @Account + '%'
                OR RTRIM(m.CName) LIKE '%' + @Account + '%')");
        // 相容舊版單一關鍵字搜尋
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
            conditions.Add(@"(RTRIM(t.IMEICode) LIKE '%' + @Keyword + '%'
                OR RTRIM(t.CName) LIKE '%' + @Keyword + '%'
                OR RTRIM(m.ID) LIKE '%' + @Keyword + '%'
                OR RTRIM(m.EMail) LIKE '%' + @Keyword + '%'
                OR RTRIM(m.CName) LIKE '%' + @Keyword + '%'
                OR RTRIM(u.UserName) LIKE '%' + @Keyword + '%')");
        if (!string.IsNullOrWhiteSpace(filter.Status))
            conditions.Add("t.TrackerEnabled = @Status");
        return conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
    }

    /// <summary>依 QueryFilter.SortBy 產生安全 ORDER BY（白名單，避免 SQL injection）。</summary>
    private static string BuildOrderByClause(QueryFilter filter)
    {
        var dir = filter.SortDesc ? "DESC" : "ASC";
        var key = (filter.SortBy ?? string.Empty).Trim().ToLowerInvariant();
        const string tie = "RTRIM(ISNULL(t.IMEICode,'')) ASC";

        return key switch
        {
            "obm" => $"ORDER BY RTRIM(ISNULL(u.UserName,'')) {dir}, {tie}",
            "imei" => $"ORDER BY RTRIM(ISNULL(t.IMEICode,'')) {dir}",
            "cname" or "name" => $"ORDER BY RTRIM(ISNULL(t.CName,'')) {dir}, {tie}",
            "bindcount" => $@"ORDER BY (
                ISNULL((SELECT COUNT(*) FROM dbo.GCM_EMAIL g WHERE g.EMAIL = m.ID), 0)
                + ISNULL((SELECT COUNT(*) FROM dbo.ClientIDRegistration c WHERE c.ID_tbkey = m.tbKey), 0)
            ) {dir}, {tie}",
            "createdate" => $"ORDER BY t.CDate {dir}, {tie}",
            "serviceenddate" or "serviceend" => $@"ORDER BY (
                SELECT TOP 1 pl.EDate
                FROM dbo.PayLog pl
                WHERE pl.Tracker_tbKey = t.tbKey
                  AND pl.SDate <= GETDATE()
                  AND pl.EDate >= DATEADD(DAY,-1,GETDATE())
                  AND pl.SDate <> pl.EDate
                ORDER BY pl.EDate DESC
            ) {dir}, {tie}",
            "account" => $"ORDER BY RTRIM(ISNULL(m.ID,'')) {dir}, {tie}",
            "currentstatus" or "status" =>
                $"ORDER BY ISNULL(t.CurrentStatus,'N') {dir}, ISNULL(t.issleep,'N') {dir}, {tie}",
            "firmware" or "firmwareversion" => $"ORDER BY RTRIM(ISNULL(t.FWVersion,'')) {dir}, {tie}",
            "iccid" => $"ORDER BY ISNULL(RTRIM(t.ICCID),'') {dir}, {tie}",
            "apn" => $"ORDER BY ISNULL(RTRIM(t.APN),'') {dir}, {tie}",
            "tbkey" => $"ORDER BY t.tbKey {dir}",
            _ => "ORDER BY RTRIM(ISNULL(t.IMEICode,'')) ASC"
        };
    }

}

