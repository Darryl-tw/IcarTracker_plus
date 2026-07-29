namespace TrackerPlus.Core.Models;

public class BatchMoveDevicesRequest
{
    public List<string> Imeis { get; set; } = [];
    public int OBMTbKey { get; set; }
    public bool ResetOnlineTime { get; set; }
    public bool DefaultPay { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public int SaleModel { get; set; }
    public DateTime? SDate { get; set; }
    public DateTime? EDate { get; set; }
    public string EndDateStatus { get; set; } = "1";
    public int FMonth { get; set; } = 1;
    public decimal Amount { get; set; }
    public int ValueAddedWeb { get; set; }
    public string SaleMemo { get; set; } = string.Empty;
    public bool IsSimBundled { get; set; } = true;
    public string IconFile { get; set; } = string.Empty;
    public int SubAdminUserTbKey { get; set; }
    public string TargetDealerLabel { get; set; } = string.Empty;
}
