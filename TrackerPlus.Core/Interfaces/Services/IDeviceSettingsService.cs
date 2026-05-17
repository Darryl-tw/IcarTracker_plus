using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Services;

public interface IDeviceSettingsService
{
    Task<DeviceSettingsDto?> GetSettingsAsync(int trackerTbKey, int memberTbKey);
    Task<DeviceDetailDto?> GetDetailAsync(int trackerTbKey, int memberTbKey, int timezoneMinutes);
    Task<OperationResult> SaveSettingsAsync(int trackerTbKey, int memberTbKey, DeviceSettingsSaveRequest request);
}

public class DeviceSettingsSaveRequest
{
    public string Cname { get; set; } = string.Empty;
    public string IconFile { get; set; } = "A";
    public List<DeviceUdFieldSave> UdFields { get; set; } = new();
    public List<int> LabelTbKeys { get; set; } = new();

    public int GpsReportTime { get; set; }
    public int ShockMode { get; set; }
    public int ShockSensitive { get; set; }

    public int ApMinute { get; set; }
    public string ApMsg { get; set; } = string.Empty;
    public int AsMinute { get; set; }
    public string AsMsg { get; set; } = string.Empty;
    public bool ShareEnabled { get; set; }
    public string SharePassword { get; set; } = "12345678";
    public int AlertMode { get; set; }
}

public class DeviceUdFieldSave
{
    public int FieldTbKey { get; set; }
    public string Value { get; set; } = string.Empty;
}
