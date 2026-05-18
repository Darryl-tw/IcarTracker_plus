using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Repository.Mapping;

internal static class TrackerMapper
{
    private const string TrackerSelectSql = @"
        SELECT
            t.tbKey AS TbKey,
            RTRIM(t.IMEICode) AS IMEICode,
            t.Member_tbKey,
            RTRIM(t.CName) AS CName,
            t.TrackerEnabled,
            t.PowerSavingMode,
            RTRIM(t.SOSTel1) AS SOSTel1,
            t.CMemo,
            RTRIM(t.IconFile) AS IconFile,
            RTRIM(t.FWVersion) AS FWVersion,
            t.TrackerVerifyExpireDate,
            t.LastReportDate,
            t.LastConnectedDate,
            t.Lastlogintime,
            ti.Lat, ti.LatPos, ti.Lng, ti.LngPos,
            ti.Speed, ti.Direction, ti.QTY_GPS, ti.SatelliteStatus, ti.OtherStatus, ti.GPSDateTime,
            t.FC_Lat, t.FC_Lng, t.FC_R, t.isFCEnable,
            t.FC_Lat1, t.FC_Lng1, t.FC_R1, t.isFCEnable1,
            t.FC_Lat2, t.FC_Lng2, t.FC_R2, t.isFCEnable2,
            t.FC_Lat3, t.FC_Lng3, t.FC_R3, t.isFCEnable3,
            ISNULL((SELECT TOP 1 CAST(pl.ValueAddedWeb AS INT)
                FROM dbo.PayLog pl
                WHERE pl.Tracker_tbKey = t.tbKey
                  AND pl.SDate <= GETDATE()
                  AND pl.EDate >= DATEADD(DAY,-1,GETDATE())
                  AND pl.SDate <> pl.EDate
                ORDER BY pl.EDate DESC), 0) AS ValueAddedWeb,
            (SELECT TOP 1 pl.EDate
                FROM dbo.PayLog pl
                WHERE pl.Tracker_tbKey = t.tbKey
                  AND pl.SDate <= GETDATE()
                  AND pl.EDate >= DATEADD(DAY,-1,GETDATE())
                  AND pl.SDate <> pl.EDate
                ORDER BY pl.EDate DESC) AS PayLogEndDate";

    private const string TrackerFromSql = @"
        FROM dbo.Tracker t
        LEFT JOIN dbo.Tracker_Info ti ON RTRIM(t.IMEICode) = RTRIM(ti.IMEICode)";

    public static string SelectColumns => TrackerSelectSql;
    public static string FromClause => TrackerFromSql;

    public static Tracker ToModel(TrackerDbRow row)
    {
        var lat = GpsHelper.ToDecimalDegrees(row.Lat, row.LatPos);
        var lng = GpsHelper.ToDecimalDegrees(row.Lng, row.LngPos);

        if (lat == 0 && lng == 0)
        {
            lat = ParseFenceCoord(row.FC_Lat);
            lng = ParseFenceCoord(row.FC_Lng);
        }

        _ = int.TryParse((row.Direction ?? "0").Trim(), out var direction);

        return new Tracker
        {
            TbKey = row.TbKey,
            IMEICODE = row.IMEICode.Trim(),
            Member_TbKey = row.Member_tbKey,
            CName = row.CName.Trim(),
            Lat = lat,
            Lng = lng,
            Speed = GpsHelper.ParseSpeedKmh(row.Speed),
            Direction = direction,
            LastReportTime = row.LastReportDate ?? row.LastConnectedDate ?? row.Lastlogintime ?? row.GPSDateTime,
            TrackerStatus = row.TrackerEnabled.Trim(),
            PowerSavingMode = row.PowerSavingMode,
            SosNumber = row.SOSTel1.Trim(),
            Memo = row.CMemo?.Trim() ?? string.Empty,
            IconFile = NormalizeIconFile(row.IconFile),
            FirmwareVersion = row.FWVersion?.Trim() ?? string.Empty,
            ServiceEndDate = row.PayLogEndDate,
            WebPlatform = row.ValueAddedWeb == 1 ? "專業" : "標準",
            Voltage = GpsHelper.ParseVoltagePercent(row.OtherStatus),
            GPSNo = GpsHelper.ParseGpsSatelliteCount(row.QTY_GPS),
            VoltageDisplay = GpsHelper.ParseVoltageStr(row.OtherStatus),
            GpsSatellitesUsed = GpsHelper.CountActiveSatellites(row.SatelliteStatus),
            GpsSatellitesTotal = 12,
            FC0_Lat = row.FC_Lat?.Trim() ?? string.Empty,
            FC0_Lng = row.FC_Lng?.Trim() ?? string.Empty,
            FC0_Radius = ParseRadius(row.FC_R),
            FC0_Enable = NormalizeEnable(row.isFCEnable),
            FC1_Lat = row.FC_Lat1?.Trim() ?? string.Empty,
            FC1_Lng = row.FC_Lng1?.Trim() ?? string.Empty,
            FC1_Radius = ParseRadius(row.FC_R1),
            FC1_Enable = NormalizeEnable(row.isFCEnable1),
            FC2_Lat = row.FC_Lat2?.Trim() ?? string.Empty,
            FC2_Lng = row.FC_Lng2?.Trim() ?? string.Empty,
            FC2_Radius = ParseRadius(row.FC_R2),
            FC2_Enable = NormalizeEnable(row.isFCEnable2),
            FC3_Lat = row.FC_Lat3?.Trim() ?? string.Empty,
            FC3_Lng = row.FC_Lng3?.Trim() ?? string.Empty,
            FC3_Radius = ParseRadius(row.FC_R3),
            FC3_Enable = NormalizeEnable(row.isFCEnable3)
        };
    }

    private static double ParseFenceCoord(string? value)
    {
        if (double.TryParse((value ?? string.Empty).Trim(), out var v) && v != 0)
            return v;
        return 0;
    }

    private static double ParseRadius(string? value)
        => double.TryParse((value ?? "0").Trim(), out var r) ? r : 0;

    private static string NormalizeEnable(string? value)
    {
        var v = (value ?? "0").Trim();
        return v is "1" or "Y" ? "Y" : "N";
    }

    private static string NormalizeIconFile(string? value)
    {
        var v = (value ?? "A").Trim().ToUpperInvariant();
        return v is "A" or "B" or "C" or "D" or "E" or "F" or "G" or "H" ? v : "A";
    }
}
