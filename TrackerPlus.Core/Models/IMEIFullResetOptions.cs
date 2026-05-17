namespace TrackerPlus.Core.Models;

/// <summary>IMEI 完整重置選項（對應舊版 IMEI_Reset.aspx）</summary>
public class IMEIFullResetOptions
{
    /// <summary>刪除歷史並寫入 DelHistoryTLGAll</summary>
    public bool DeleteHistory { get; set; } = true;

    /// <summary>重置 PayLog（否則僅更新 Tracker_tbKey）</summary>
    public bool ResetPayLog { get; set; }

    public int ObmTbKey { get; set; } = 1;
}
