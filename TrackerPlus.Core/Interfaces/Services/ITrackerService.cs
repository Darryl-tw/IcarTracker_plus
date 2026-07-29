using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Services;

public interface ITrackerService
{
    Task<Tracker?> GetTrackerAsync(int tbKey);
    Task<Tracker?> GetTrackerByIMEIAsync(string imei);
    Task<IEnumerable<Tracker>> GetMemberTrackersAsync(int memberTbKey);
    Task<PagedResult<Tracker>> GetTrackersPagedAsync(QueryFilter filter, int? memberTbKey = null);
    Task<OperationResult> CreateTrackerAsync(Tracker tracker);
    Task<OperationResult> UpdateTrackerAsync(Tracker tracker);
    Task<OperationResult> DeleteTrackerAsync(int tbKey);
    Task<OperationResult> DeleteAllTrackersByMemberAsync(int memberTbKey);
    Task<OperationResult> UpdateTrackerInfoAsync(int tbKey, string cname, string memo, string label, string groupName);
    Task<OperationResult> UpdateTrackerOptionAsync(int tbKey, string sosNumber, string powerSavingMode);
    Task<OperationResult> UpdateTrackerLabelsAsync(int tbKey, int memberTbKey, IEnumerable<int> labelTbKeys);
    Task<OperationResult> SaveDeviceSettingsAsync(int tbKey, int memberTbKey, string cname, string memo, string iconFile, IEnumerable<int> labelTbKeys);
    Task<IEnumerable<Tracker>> GetLiveLocationsAsync(int memberTbKey);
    Task<int> GetTrackerCountAsync(int? memberTbKey = null);
    Task<OperationResult> BatchMoveDevicesAsync(BatchMoveDevicesRequest request);
    Task<OperationResult> FactoryResetAsync(int tbKey);
    Task<int> BatchDeleteByKeysAsync(IEnumerable<int> tbKeys);
    Task<OperationResult> UnbindDeviceAsync(int tbKey);
    Task<OperationResult> UnbindAllByMemberAsync(int memberTbKey);
}
