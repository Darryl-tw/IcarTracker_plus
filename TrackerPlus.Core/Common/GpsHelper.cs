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

    /// <summary>
    /// Counts active GPS satellites from SatelliteStatus encoded string.
    /// Each 2-char pair: [PRN char][Signal char]. Signal='0' means no tracking.
    /// Active count = pairs where signal char != '0'.
    /// </summary>
    public static int CountActiveSatellites(string? satelliteStatus)
    {
        var s = (satelliteStatus ?? "").Trim();
        int count = 0;
        for (int i = 1; i < s.Length; i += 2)
            if (s[i] != '0') count++;
        return count;
    }

    public static int ParseCsq(string? otherStatus)
    {
        var status = (otherStatus ?? string.Empty).Trim();
        if (status.Length < 2) return 0;
        return int.TryParse(status.Substring(0, 2).Trim(), out var csq) ? csq : 0;
    }

    /// <summary>
    /// Returns raw voltage string from OtherStatus positions 2-5 (e.g., "4.14V").
    /// Old system stores voltage as a 4-char decimal string like "4.14" or "0000".
    /// </summary>
    public static string ParseVoltageStr(string? otherStatus)
    {
        var status = (otherStatus ?? string.Empty).Trim();
        if (status.Length < 3) return "";
        var raw = status.Length >= 6 ? status.Substring(2, 4) : status.Substring(2);
        raw = raw.Trim();
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || value <= 0)
            return "";
        return value.ToString("F2", CultureInfo.InvariantCulture) + "V";
    }

    public static int ParseVoltagePercent(string? otherStatus)
    {
        var status = (otherStatus ?? string.Empty).Trim();
        if (status.Length < 3) return 0;
        var raw = status.Length >= 6 ? status.Substring(2, 4) : status.Substring(2);
        raw = raw.Trim();
        // OtherStatus voltage is stored as a decimal string like "4.14" (volts)
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || value <= 0) return 0;
        // ≤5V → lithium battery (0–4.2V range); >5V → car battery (0–15V range)
        if (value <= 5) return Math.Clamp((int)Math.Round(value / 4.2 * 100), 0, 100);
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
