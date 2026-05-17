namespace TrackerPlus.Core.Models;

/// <summary>警報記錄</summary>
public class AlertLog
{
    public long TbKey { get; set; }
    public string IMEICODE { get; set; } = string.Empty;
    public DateTime UTCTime { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsRead { get; set; }
}

/// <summary>系統操作記錄</summary>
public class SystemLog
{
    public long TbKey { get; set; }
    public string IMEICODE { get; set; } = string.Empty;
    public DateTime UTCTime { get; set; } = DateTime.UtcNow;
    public string LogType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
}
