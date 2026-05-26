namespace TrackerPlus.Repository.Mapping;

internal sealed class PayLogDbRow
{
    public int TbKey { get; set; }
    public int Member_tbKey { get; set; }
    public int Tracker_tbKey { get; set; }
    public string IMEICode { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime? SDate { get; set; }
    public DateTime? EDate { get; set; }
    public DateTime CDate { get; set; }
    public string? CMemo { get; set; }
    public int Model { get; set; }
    public string? sale_memo { get; set; }
    public int ValueAddedWeb { get; set; }
    public string? MemberName { get; set; }
    public int? OBM_tbKey { get; set; }
    public string? OBMName { get; set; }
}
