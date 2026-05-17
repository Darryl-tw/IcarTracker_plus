using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Repositories;

public interface IDeviceSettingsRepository
{
    Task<DeviceSettingsDto?> GetSettingsAsync(int trackerTbKey, int memberTbKey);
    Task<DeviceDetailDto?> GetDetailAsync(int trackerTbKey, int memberTbKey, int timezoneMinutes);
    Task<bool> SaveInfoAsync(int trackerTbKey, string cname, string iconFile, IEnumerable<(int FieldTbKey, string Value)> udValues, IEnumerable<int> labelTbKeys);
    Task<bool> SaveOptionAsync(int trackerTbKey, int gpsReportTime, int shockMode, int shockSensitive);
    Task<bool> SaveAlertAsync(int trackerTbKey, int memberTbKey, int apMinute, string apMsg, int asMinute, string asMsg, bool shareEnabled, string sharePassword, int alertMode);
}
