namespace TrackerPlus.Core.Models;

/// <summary>追蹤器主資料</summary>
public class Tracker
{
    public int TbKey { get; set; }
    public string IMEICODE { get; set; } = string.Empty;
    public int Member_TbKey { get; set; }
    public string CName { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double Speed { get; set; }
    public int Direction { get; set; }
    public DateTime? LastReportTime { get; set; }
    public string TrackerStatus { get; set; } = "Y";
    /// <summary>省電模式 (DB bit 型別)</summary>
    public bool PowerSavingMode { get; set; } = false;
    /// <summary>電子柵欄0-3的設定</summary>
    public string FC0_Lat { get; set; } = string.Empty;
    public string FC0_Lng { get; set; } = string.Empty;
    public double FC0_Radius { get; set; }
    public string FC0_Enable { get; set; } = "N";
    public string FC1_Lat { get; set; } = string.Empty;
    public string FC1_Lng { get; set; } = string.Empty;
    public double FC1_Radius { get; set; }
    public string FC1_Enable { get; set; } = "N";
    public string FC2_Lat { get; set; } = string.Empty;
    public string FC2_Lng { get; set; } = string.Empty;
    public double FC2_Radius { get; set; }
    public string FC2_Enable { get; set; } = "N";
    public string FC3_Lat { get; set; } = string.Empty;
    public string FC3_Lng { get; set; } = string.Empty;
    public double FC3_Radius { get; set; }
    public string FC3_Enable { get; set; } = "N";
    public string SosNumber { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;
    /// <summary>地圖圖示代碼 A–H（對應 IconA–IconH）</summary>
    public string IconFile { get; set; } = "A";
    /// <summary>服務到期日</summary>
    public DateTime? ServiceEndDate { get; set; }
    /// <summary>網頁平台：標準 / 專業</summary>
    public string WebPlatform { get; set; } = "標準";
    public int Voltage { get; set; }
    public int CSQ { get; set; }
    public int GPSNo { get; set; }
}
