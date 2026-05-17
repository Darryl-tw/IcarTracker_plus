namespace TrackerPlus.Core.Models;

public class DeviceSettingsDto
{
    public int TbKey { get; set; }
    public string Imei { get; set; } = string.Empty;
    public string CName { get; set; } = string.Empty;
    public string IconFile { get; set; } = "A";
    public string Memo { get; set; } = string.Empty;

    public List<DeviceUdFieldDto> UdFields { get; set; } = new();
    public List<DeviceLabelDto> Labels { get; set; } = new();

    public int GpsReportTime { get; set; }
    public string SmsPassword { get; set; } = string.Empty;
    public int ShockMode { get; set; }
    public int ShockSensitive { get; set; }

    public int ApMinute { get; set; }
    public string ApMsg { get; set; } = string.Empty;
    public int AsMinute { get; set; }
    public string AsMsg { get; set; } = string.Empty;
    public bool ShareEnabled { get; set; }
    public string SharePassword { get; set; } = "12345678";
    public int AlertMode { get; set; }

    public List<DevicePayLogRowDto> PayLogs { get; set; } = new();
}

public class DeviceUdFieldDto
{
    public int FieldTbKey { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class DeviceLabelDto
{
    public int TbKey { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Assigned { get; set; }
}

public class DevicePayLogRowDto
{
    public DateTime? ServiceStart { get; set; }
    public DateTime? ServiceEnd { get; set; }
}

public class DeviceDetailDto
{
    public int TbKey { get; set; }
    public string Imei { get; set; } = string.Empty;
    public string CName { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public string? LastLoginLocal { get; set; }
    public string? LastReportLocal { get; set; }
    public string? LastConnectedLocal { get; set; }
    public string? OfflineDuration { get; set; }
    public double SpeedKmh { get; set; }
    public double DistanceMeters { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string? Address { get; set; }
    public string DirectionText { get; set; } = string.Empty;
    public int Csq { get; set; }
    public int GpsUsed { get; set; }
    public int GpsTotal { get; set; }
    public int VoltagePercent { get; set; }
    public double VoltageV { get; set; }
}
