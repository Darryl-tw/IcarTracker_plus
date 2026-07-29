using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Repositories;

public interface ITrackerRepository
{
    Task<Tracker?> GetByIdAsync(int tbKey);
    Task<Tracker?> GetByIMEIAsync(string imei);
    Task<IEnumerable<Tracker>> GetByMemberAsync(int memberTbKey);
    Task<PagedResult<Tracker>> GetPagedAsync(QueryFilter filter, int? memberTbKey = null);
    Task<int> CreateAsync(Tracker tracker);
    Task<bool> UpdateAsync(Tracker tracker);
    Task<bool> UpdateLiveSettingsAsync(int tbKey, string cname, string memo, string iconFile);
    Task<bool> DeleteAsync(int tbKey);
    Task<bool> UpdateStatusAsync(int tbKey, string status);
    Task<bool> UpdateGeofenceAsync(int tbKey, Geofence geofence);
    Task<bool> UpdateLiveLocationAsync(string imei, double lat, double lng, double speed, int direction, DateTime utcTime);
    Task<IEnumerable<Tracker>> GetByGroupAsync(int memberTbKey, string groupName);
    Task<int> GetCountAsync(int? memberTbKey = null);
    Task<OperationResult> BatchMoveDevicesAsync(BatchMoveDevicesRequest request);
    Task<OperationResult> FactoryResetAsync(int tbKey);
    Task<int> BatchDeleteByKeysAsync(IEnumerable<int> tbKeys);

    /// <summary>後台新增裝置（對齊舊 IMEI_New：IMEITable / Tracker / Tracker_Info / AlertModule / Log 表 / 初始化 PayLog）。</summary>
    Task<OperationResult> InsertDevicesAsync(IEnumerable<string> imeis, int subAdminUserTbKey);

    Task<(bool Success, string? ErrorCode)> DeleteDeviceByImeiAsync(string imei, int subAdminUserTbKey, string? adminFunction = null);
    Task<OperationResult> UnbindAsync(int tbKey);
    Task<OperationResult> UnbindAllByMemberAsync(int memberTbKey);
}
