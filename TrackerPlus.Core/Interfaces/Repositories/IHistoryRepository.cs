using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Repositories;

public interface IHistoryRepository
{
    Task<TrackingHistoryResult> GetGPSHistoryAsync(string imei, DateTime startUtc, DateTime endUtc, int pageIndex, int pageSize);
    Task<TrackingHistoryResult> GetLBSHistoryAsync(string imei, DateTime startUtc, DateTime endUtc, int pageIndex, int pageSize);
    Task<TrackingHistoryResult> GetWifiHistoryAsync(string imei, DateTime startUtc, DateTime endUtc, int pageIndex, int pageSize);
    Task<IEnumerable<TrackingLog>> GetGPSLogsForExportAsync(string imei, DateTime startUtc, DateTime endUtc);
    Task<IEnumerable<LBSLog>> GetLBSLogsForExportAsync(string imei, DateTime startUtc, DateTime endUtc);
    Task<bool> DeleteHistoryAsync(string imei, DateTime startUtc, DateTime endUtc);
    Task<TrackingLog?> GetLatestPositionAsync(string imei);
    Task<IEnumerable<AlertLog>> GetAlertLogsAsync(string imei, DateTime startUtc, DateTime endUtc);
    Task<IEnumerable<DailyHistorySummary>> GetDailySummaryAsync(string imei, DateTime startUtc, DateTime endUtc);
}
