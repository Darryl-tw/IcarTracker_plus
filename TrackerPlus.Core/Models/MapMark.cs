namespace TrackerPlus.Core.Models;

/// <summary>會員自建地標 (tb_MapMark)</summary>
public class MapMark
{
    public int TbKey { get; set; }
    public int MemberTbKey { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public DateTime CreatedAt { get; set; }
}
