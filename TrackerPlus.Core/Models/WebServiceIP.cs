namespace TrackerPlus.Core.Models;

/// <summary>WebService IP 白名單</summary>
public class WebServiceIP
{
    public int TbKey { get; set; }
    public string IPAddress { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Y";
    public DateTime CDate { get; set; } = DateTime.UtcNow;
}
