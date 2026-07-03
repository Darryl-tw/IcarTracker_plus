namespace TrackerPlus.Core.Models;

/// <summary>經銷商資料 (對應 dbo.Userdb)</summary>
public class Dealer
{
    public int TbKey { get; set; }
    public string UserCode { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserID { get; set; } = string.Empty;
    public string UserPW { get; set; } = string.Empty;
    public DateTime? CreateDate { get; set; }
    public string Status { get; set; } = "Y";
    public bool PaymentService { get; set; }
    public string UserMemo { get; set; } = string.Empty;
    public string Tel { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public string Webtitle { get; set; } = string.Empty;
    public string ServiceEmail { get; set; } = string.Empty;
    public string ServiceSenderTitle { get; set; } = string.Empty;

    // 統計欄位（由 SQL 聚合計算）
    public int TotalDevices { get; set; }
    public int OnlineDevices { get; set; }
    public int YesterdayDevices { get; set; }
    public int YesterdayLogins { get; set; }
    public int Offline90Days { get; set; }
    public int ExpiredDevices { get; set; }
    public int BoundPhones { get; set; }
    public int BoundEmails { get; set; }
}
