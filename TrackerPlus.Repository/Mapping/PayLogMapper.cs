using TrackerPlus.Core.Models;

namespace TrackerPlus.Repository.Mapping;

internal static class PayLogMapper
{
    public const string SelectSql = @"
        SELECT
            p.tbKey AS TbKey,
            p.Member_tbKey,
            p.Tracker_tbKey,
            RTRIM(p.IMEICode) AS IMEICode,
            RTRIM(p.OrderNumber) AS OrderNumber,
            p.Amount,
            p.SDate,
            p.EDate,
            p.CDate,
            p.CMemo,
            p.Model,
            p.sale_memo,
            ISNULL(CAST(p.ValueAddedWeb AS INT), 0) AS ValueAddedWeb,
            RTRIM(m.CName) AS MemberName,
            t.OBM_tbKey,
            RTRIM(u.UserName) AS OBMName";

    public const string FromSql = @"
        FROM dbo.PayLog p
        LEFT JOIN dbo.Member m ON p.Member_tbKey = m.tbKey
        LEFT JOIN dbo.Tracker t ON p.Tracker_tbKey = t.tbKey
        LEFT JOIN dbo.Userdb u ON t.OBM_tbKey = u.tbKey";

    public static PayLog ToModel(PayLogDbRow row) => new()
    {
        TbKey = row.TbKey,
        IMEICODE = row.IMEICode.Trim(),
        Member_TbKey = row.Member_tbKey,
        OBM_TbKey = row.OBM_tbKey ?? 0,
        OrderNo = row.OrderNumber.Trim(),
        Amount = row.Amount,
        SaleModel = row.Model,
        SDate = row.SDate,
        EDate = row.EDate,
        CDate = row.CDate,
        Memo = row.CMemo?.Trim() ?? string.Empty,
        SaleMemo = row.sale_memo?.Trim() ?? string.Empty,
        ValueAddedWeb = row.ValueAddedWeb,
        Status = "Y",
        Operator = string.Empty,
        MemberName = row.MemberName?.Trim() ?? string.Empty,
        OBMName = row.OBMName?.Trim() ?? string.Empty
    };
}
