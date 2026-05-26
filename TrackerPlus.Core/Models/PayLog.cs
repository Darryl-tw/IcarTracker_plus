namespace TrackerPlus.Core.Models;

/// <summary>付款/訂閱紀錄</summary>
public class PayLog
{
    public int TbKey { get; set; }
    public string IMEICODE { get; set; } = string.Empty;
    public int Member_TbKey { get; set; }
    public int OBM_TbKey { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Commission { get; set; }
    /// <summary>1=續費, 4=國際, 5=特殊, 6=退款, 7=自動啟用, 21=多月</summary>
    public int SaleModel { get; set; }
    public DateTime? SDate { get; set; }
    public DateTime? EDate { get; set; }
    public DateTime CDate { get; set; } = DateTime.UtcNow;
    public string Memo { get; set; } = string.Empty;
    /// <summary>業務備註（sale_memo）</summary>
    public string SaleMemo { get; set; } = string.Empty;
    /// <summary>加值服務：1=網頁平台, 0=無</summary>
    public int ValueAddedWeb { get; set; }
    public string Status { get; set; } = "Y";
    /// <summary>操作人員</summary>
    public string Operator { get; set; } = string.Empty;
    /// <summary>會員名稱(Join)</summary>
    public string MemberName { get; set; } = string.Empty;
    /// <summary>OBM名稱(Join)</summary>
    public string OBMName { get; set; } = string.Empty;
}
