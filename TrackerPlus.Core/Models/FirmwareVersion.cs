namespace TrackerPlus.Core.Models;

/// <summary>韌體版本（對應 FWCONROL 表，主鍵為 FWVERSION）</summary>
public class FirmwareVersion
{
    public string FWVERSION { get; set; } = string.Empty;
    public string FtpServer { get; set; } = string.Empty;
    public string FtpUsername { get; set; } = string.Empty;
    public string FtpPassword { get; set; } = string.Empty;
    public string FtpDir { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileSize { get; set; } = string.Empty;
    /// <summary>NEW_FWVERSION 欄位（版本說明／備註）</summary>
    public string NewFwVersion { get; set; } = string.Empty;
    public DateTime CDate { get; set; } = DateTime.UtcNow;

    /// <summary>相容舊 UI 綁定</summary>
    public string Version
    {
        get => FWVERSION;
        set => FWVERSION = value;
    }

    public string ModelName
    {
        get => NewFwVersion;
        set => NewFwVersion = value;
    }

    public string Description
    {
        get => NewFwVersion;
        set => NewFwVersion = value;
    }
}

/// <summary>韌體升級批次（Tracker.update_FWVERSION）</summary>
public class FirmwareUpdateTask
{
    public string IMEICODE { get; set; } = string.Empty;
    public string TargetVersion { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = string.Empty;
    public DateTime CDate { get; set; } = DateTime.UtcNow;
}
