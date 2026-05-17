using System.Globalization;
using System.Text.RegularExpressions;

namespace TrackerPlus.Core.Common;

/// <summary>與舊版 Tracker (VB TrackerFUN) 一致的 GPS 座標與狀態解析。</summary>
public static class GpsHelper
{
    private static readonly Regex LatitudeRegex = new(@"^[-+]?([1-8]?\d(\.\d+)?|90(\.0+)?)$", RegexOptions.Compiled);
    private static readonly Regex LongitudeRegex = new(@"^[-+]?(180(\.0+)?|((1[0-7]\d)|([1-9]?\d))(\.\d+)?)$", RegexOptions.Compiled);

    public static double ToDecimalDegrees(string? hemisphere, string? position)
    {
        var pos = (position ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(pos)) return 0;

        if (double.TryParse(pos, NumberStyles.Float, CultureInfo.InvariantCulture, out var direct))
        {
            if (IsValidLatitude(direct) || IsValidLongitude(direct))
                return ApplyHemisphere(direct, hemisphere);
        }

        var converted = ConvertNmeaToDecimal(pos);
        return ApplyHemisphere(converted, hemisphere);
    }

    public static double ConvertNmeaToDecimal(string nmea)
    {
        nmea = nmea.Trim();
        if (double.TryParse(nmea, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            if (LatitudeRegex.IsMatch(nmea) || LongitudeRegex.IsMatch(nmea))
                return value;
        }

        var dotIndex = nmea.IndexOf('.');
        if (dotIndex < 2) return 0;

        var degLen = dotIndex - 2;
        if (degLen <= 0 || !double.TryParse(nmea[..degLen], out var degrees)) return 0;
        if (!double.TryParse(nmea[degLen..], NumberStyles.Float, CultureInfo.InvariantCulture, out var minutes))
            return 0;

        return degrees + minutes / 60.0;
    }

    public static int ParseGmtCodeToMinutes(string? gmtCode)
    {
        var code = (gmtCode ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(code)) return 480;

        var sign = code.StartsWith('-') ? -1 : 1;
        code = code.TrimStart('+', '-');
        var parts = code.Split(':');
        if (!int.TryParse(parts[0], out var hours)) return 480;
        var minutes = parts.Length > 1 && int.TryParse(parts[1], out var m) ? m : 0;
        return sign * (hours * 60 + minutes);
    }

    public static int ParseCsq(string? otherStatus)
    {
        var status = (otherStatus ?? string.Empty).Trim();
        if (status.Length < 2) return 0;
        return int.TryParse(status.Substring(0, 2).Trim(), out var csq) ? csq : 0;
    }

    public static int ParseVoltagePercent(string? otherStatus)
    {
        var status = (otherStatus ?? string.Empty).Trim();
        if (status.Length < 6) return 0;
        var raw = status.Substring(2, 4).Trim();
        if (!int.TryParse(raw, out var value)) return 0;
        // 舊系統：≤5 視為鋰電百分比等級，>5 為車電電壓值；UI 以百分比顯示時做簡化換算
        if (value <= 5) return Math.Clamp(value * 20, 0, 100);
        return Math.Clamp((int)Math.Round(value / 15.0 * 100), 0, 100);
    }

    public static int ParseGpsSatelliteCount(string? qtyGps)
    {
        if (int.TryParse((qtyGps ?? string.Empty).Trim(), out var count))
            return count;
        return 0;
    }

    public static double ParseSpeedKmh(string? speed)
    {
        if (!double.TryParse((speed ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var knots))
            return 0;
        return Math.Round(knots * 1.85, 1);
    }

    private static double ApplyHemisphere(double value, string? hemisphere)
    {
        var h = (hemisphere ?? string.Empty).Trim().ToUpperInvariant();
        if (h is "S" or "W") return -Math.Abs(value);
        return Math.Abs(value);
    }

    private static bool IsValidLatitude(double lat) => Math.Abs(lat) <= 90;
    private static bool IsValidLongitude(double lng) => Math.Abs(lng) <= 180;

    public static string DirectionTextFromDegrees(int degrees)
    {
        var x = ((degrees % 360) + 360) % 360;
        return x switch
        {
            > 348 or <= 11 => "北",
            <= 33 => "北北東",
            <= 56 => "東北",
            <= 78 => "東北東",
            <= 101 => "東",
            <= 123 => "東南東",
            <= 146 => "東南",
            <= 168 => "南南東",
            <= 191 => "南",
            <= 213 => "南南西",
            <= 236 => "西南",
            <= 258 => "西南西",
            <= 281 => "西",
            <= 303 => "西北西",
            <= 326 => "西北",
            _ => "北北西"
        };
    }
}
