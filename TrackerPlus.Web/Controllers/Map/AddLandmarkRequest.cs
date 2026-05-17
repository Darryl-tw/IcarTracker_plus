namespace TrackerPlus.Web.Controllers;

public class AddLandmarkRequest
{
    public string? Memo { get; set; }
    public string? Address { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
}
