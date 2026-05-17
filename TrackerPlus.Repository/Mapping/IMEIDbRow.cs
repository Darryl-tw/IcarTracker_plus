namespace TrackerPlus.Repository.Mapping;

internal sealed class IMEIDbRow
{
    public int TbKey { get; set; }
    public int OBM_tbKey { get; set; }
    public string IMEICODE { get; set; } = string.Empty;
    public string STATUS { get; set; } = "N";
    public int Tracker_tbKey { get; set; }
    public string? TK_Model { get; set; }
    public string? CMemo { get; set; }
    public DateTime? CDate { get; set; }
    public int? Member_tbKey { get; set; }
    public string? MemberName { get; set; }
    public string? FWVersion { get; set; }
}
