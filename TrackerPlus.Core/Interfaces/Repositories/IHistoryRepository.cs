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
    Task<IEnumerable<DailyHistorySummary>> GetDailySummaryAsync(string imei, DateTime startUtc, DateTime endUtc, int timezoneMinutes);
    IAsyncEnumerable<TrackingLog> StreamGPSHistoryAsync(string imei, DateTime startUtc, DateTime endUtc, CancellationToken ct = default);
    IAsyncEnumerable<TrackingLog> StreamLBSHistoryAsync(string imei, DateTime startUtc, DateTime endUtc, CancellationToken ct = default);
    IAsyncEnumerable<TrackingLog> StreamCombinedHistoryAsync(string imei, DateTime startUtc, DateTime endUtc, CancellationToken ct = default);
    IAsyncEnumerable<DailyHistorySummary> StreamDailySummaryAsync(string imei, DateTime startUtc, DateTime endUtc, int timezoneMinutes, CancellationToken ct = default);
    IAsyncEnumerable<DailyHistorySummary> StreamCombinedDailySummaryAsync(string imei, DateTime startUtc, DateTime endUtc, int timezoneMinutes, CancellationToken ct = default);
    Task<bool> QueueMoveHistoryAsync(string sourceImei, string destImei);
}
