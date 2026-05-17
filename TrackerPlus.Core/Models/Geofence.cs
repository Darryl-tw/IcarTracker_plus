namespace TrackerPlus.Core.Models;

/// <summary>電子柵欄設定</summary>
public class Geofence
{
    public int TbKey { get; set; }
    public int Tracker_TbKey { get; set; }
    public string IMEICODE { get; set; } = string.Empty;
    /// <summary>柵欄索引 0-3</summary>
    public int FenceIndex { get; set; }
    public string CName { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double Radius { get; set; }
    /// <summary>Y=啟用, N=停用</summary>
    public string Enable { get; set; } = "N";
    /// <summary>進入=I, 離開=O, 兩者=B</summary>
    public string AlertType { get; set; } = "B";
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
}
