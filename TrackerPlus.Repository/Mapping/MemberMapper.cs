using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Repository.Mapping;

internal static class MemberMapper
{
    public const string SelectSql = @"
        SELECT m.tbKey, m.OBM_tbKey, RTRIM(m.ID) AS ID, RTRIM(m.EMail) AS EMail, m.Password,
               RTRIM(m.CName) AS CName, m.MemberStatus, RTRIM(m.DynaCheckCode) AS DynaCheckCode,
               RTRIM(m.UserLanguage) AS UserLanguage, RTRIM(m.Unit) AS Unit, m.TimeZone_tbkey,
               RTRIM(m.Tel) AS Tel, RTRIM(m.Addr) AS Addr, m.ConnectionTime,
               m.issendemail, m.ispush, m.CDate, tz.GMTCode
        FROM dbo.Member m
        LEFT JOIN dbo.TimeZone tz ON m.TimeZone_tbkey = tz.tbKey";

    public static Member ToModel(MemberDbRow row) => new()
    {
        TbKey = row.TbKey,
        OBM_TbKey = row.OBM_tbKey,
        ID = row.ID.Trim(),
        Email = row.EMail.Trim(),
        Password = row.Password,
        CName = row.CName.Trim(),
        MemberStatus = row.MemberStatus.Trim(),
        DynaCheckCode = row.DynaCheckCode.Trim(),
        UserLanguage = row.UserLanguage.Trim(),
        UserUnit = row.Unit.Trim(),
        Timezoom = GpsHelper.ParseGmtCodeToMinutes(row.GMTCode),
        Tel = row.Tel.Trim(),
        Addr = row.Addr.Trim(),
        ConnectionTime = row.ConnectionTime,
        IsSendEmail = row.issendemail,
        IsPush = row.ispush,
        CDate = row.CDate
    };
}
