namespace TrackerPlus.Core.Models;

/// <summary>GPS歷史記錄單筆資料</summary>
public class TrackingLog
{
    public long TbKey { get; set; }
    public string IMEICODE { get; set; } = string.Empty;
    /// <summary>G=GPS, L=LBS基站, W=WiFi</summary>
    public string Type { get; set; } = "G";
    public DateTime UTCTime { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double Speed { get; set; }
    public int Direction { get; set; }
    public double Distance { get; set; }
    public int Voltage { get; set; }
    public int CSQ { get; set; }
    public int GPSNo { get; set; }
    public double HDOP { get; set; }
    public int GPSSignal { get; set; }
    public int GlonassNo { get; set; }
    /// <summary>地址(反查後填入)</summary>
    public string Address { get; set; } = string.Empty;
}

/// <summary>歷史查詢分頁結果</summary>
public class TrackingHistoryResult
{
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public List<TrackingLog> Logs { get; set; } = new();
    public double TotalDistance { get; set; }
    public double MaxSpeed { get; set; }
    public double AvgSpeed { get; set; }
}

/// <summary>LBS基站定位記錄</summary>
public class LBSLog
{
    public long TbKey { get; set; }
    public string IMEICODE { get; set; } = string.Empty;
    public DateTime UTCTime { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public int MCC { get; set; }
    public int MNC { get; set; }
    public int LAC { get; set; }
    public int CellID { get; set; }
    public string Address { get; set; } = string.Empty;
}

/// <summary>每日歷史行蹤摘要</summary>
public class DailyHistorySummary
{
    public DateTime Date { get; set; }
    public DateTime? FirstGPS { get; set; }
    public DateTime? LastGPS { get; set; }
    public int RecordCount { get; set; }
    public double TotalDistanceKm { get; set; }
}

/// <summary>WiFi定位記錄</summary>
public class WifiLog
{
    public long TbKey { get; set; }
    public string IMEICODE { get; set; } = string.Empty;
    public DateTime UTCTime { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string SSID { get; set; } = string.Empty;
    public string BSSID { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}
