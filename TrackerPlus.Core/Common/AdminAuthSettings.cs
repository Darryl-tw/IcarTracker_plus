namespace TrackerPlus.Core.Common;

/// <summary>後台登入逾時設定（帳密由資料庫 dbo.Userdb 驗證，對齊舊系統）。</summary>
public class AdminAuthSettings
{
    /// <summary>勾選「記住我」時 Cookie 有效分鐘數（舊系統：8 小時）。</summary>
    public int RememberMeMinutes { get; set; } = 480;
    /// <summary>未勾選「記住我」時 Cookie 有效分鐘數（舊系統：30 分鐘）。</summary>
    public int SessionMinutes { get; set; } = 30;
    /// <summary>後台無操作自動登出分鐘數（舊系統 index.aspx：10 分鐘）。</summary>
    public int IdleTimeoutMinutes { get; set; } = 10;
}
