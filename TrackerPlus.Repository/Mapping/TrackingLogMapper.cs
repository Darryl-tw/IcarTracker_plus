using System.Globalization;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Repository.Mapping;

internal static class TrackingLogMapper
{
    public static TrackingLog ToModel(TrackingLogDbRow row)
    {
        _ = int.TryParse((row.Direction ?? "0").Trim(), out var direction);
        _ = double.TryParse((row.Distance ?? "0").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var distance);
        _ = double.TryParse((row.HDOP ?? "0").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var hdop);

        return new TrackingLog
        {
            IMEICODE = row.IMEICode.Trim(),
            UTCTime = row.UTCTime,
            Lat = GpsHelper.ToDecimalDegrees(row.Lat, row.LatPos),
            Lng = GpsHelper.ToDecimalDegrees(row.Lng, row.LngPos),
            Speed = GpsHelper.ParseSpeedKmh(row.Speed),
            Direction = direction,
            Distance = distance,
            HDOP = hdop,
            Voltage = GpsHelper.ParseVoltagePercent(row.OtherStatus),
            GPSNo = GpsHelper.ParseGpsSatelliteCount(row.QTY_GPS),
            CSQ = GpsHelper.ParseCsq(row.OtherStatus),
            Type = row.Type
        };
    }
}
