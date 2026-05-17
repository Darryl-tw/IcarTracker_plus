namespace TrackerPlus.Core.Models;

/// <summary>經銷商(OBM)資料</summary>
public class OBM
{
    public int TbKey { get; set; }
    public string CName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Tel { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Addr { get; set; } = string.Empty;
    public string Status { get; set; } = "Y";
    public DateTime CDate { get; set; } = DateTime.UtcNow;
    public decimal CommissionRate { get; set; }
    public string Memo { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string WebTitle { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;
}
