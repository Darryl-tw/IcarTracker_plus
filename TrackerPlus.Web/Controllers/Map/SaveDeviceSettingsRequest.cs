namespace TrackerPlus.Web.Controllers;

public class SaveDeviceSettingsRequest
{
    public int TbKey { get; set; }
    public string? Cname { get; set; }
    public string? IconFile { get; set; }
    public int[]? LabelTbKeys { get; set; }
    public UdFieldSaveItem[]? UdFields { get; set; }

    public int GpsReportTime { get; set; } = 30;
    public int ShockMode { get; set; } = 1;
    public int ShockSensitive { get; set; } = 5;

    public int ApMinute { get; set; }
    public string? ApMsg { get; set; }
    public int AsMinute { get; set; }
    public string? AsMsg { get; set; }
    public bool ShareEnabled { get; set; }
    public string? SharePassword { get; set; }
    public int AlertMode { get; set; }
}

public class UdFieldSaveItem
{
    public int FieldTbKey { get; set; }
    public string? Value { get; set; }
}
