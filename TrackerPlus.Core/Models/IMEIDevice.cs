namespace TrackerPlus.Core.Models;

/// <summary>IMEI 裝置庫存記錄</summary>
public class IMEIDevice
{
    public int TbKey { get; set; }
    public string IMEICODE { get; set; } = string.Empty;
    public int Member_TbKey { get; set; }
    public int OBM_TbKey { get; set; }
    public string Status { get; set; } = "N";
    public DateTime? CDate { get; set; }
    public DateTime? ADate { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;
    /// <summary>會員名稱(Join)</summary>
    public string MemberName { get; set; } = string.Empty;
    /// <summary>OBM名稱(Join)</summary>
    public string OBMName { get; set; } = string.Empty;
}
