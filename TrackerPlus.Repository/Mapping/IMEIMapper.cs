using TrackerPlus.Core.Models;

namespace TrackerPlus.Repository.Mapping;

internal static class IMEIMapper
{
    public const string SelectSql = @"
        SELECT
            i.tbKey AS TbKey,
            i.OBM_tbKey,
            RTRIM(i.IMEICODE) AS IMEICODE,
            RTRIM(i.STATUS) AS STATUS,
            i.Tracker_tbKey,
            RTRIM(i.TK_Model) AS TK_Model,
            i.CMemo,
            i.CDate,
            t.Member_tbKey,
            RTRIM(m.CName) AS MemberName,
            RTRIM(t.FWVersion) AS FWVersion";

    public const string FromSql = @"
        FROM dbo.IMEITable i
        LEFT JOIN dbo.Tracker t ON i.Tracker_tbKey = t.tbKey
        LEFT JOIN dbo.Member m ON t.Member_tbKey = m.tbKey";

    public static IMEIDevice ToModel(IMEIDbRow row) => new()
    {
        TbKey = row.TbKey,
        IMEICODE = row.IMEICODE.Trim(),
        OBM_TbKey = row.OBM_tbKey,
        Member_TbKey = row.Member_tbKey ?? 0,
        Status = row.STATUS.Trim(),
        ModelName = row.TK_Model?.Trim() ?? string.Empty,
        FirmwareVersion = row.FWVersion?.Trim() ?? string.Empty,
        Memo = row.CMemo?.Trim() ?? string.Empty,
        CDate = row.CDate,
        MemberName = row.MemberName?.Trim() ?? string.Empty
    };
}
