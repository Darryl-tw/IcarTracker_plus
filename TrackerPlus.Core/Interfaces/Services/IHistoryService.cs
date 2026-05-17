using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Services;

public interface IHistoryService
{
    Task<TrackingHistoryResult> GetGPSHistoryAsync(string imei, DateTime localStart, DateTime localEnd, int memberTimezone, int pageIndex, int pageSize);
    Task<TrackingHistoryResult> GetLBSHistoryAsync(string imei, DateTime localStart, DateTime localEnd, int memberTimezone, int pageIndex, int pageSize);
    Task<TrackingHistoryResult> GetWifiHistoryAsync(string imei, DateTime localStart, DateTime localEnd, int memberTimezone, int pageIndex, int pageSize);
    Task<byte[]> ExportToExcelAsync(string imei, DateTime localStart, DateTime localEnd, int memberTimezone, string type = "GPS");
    Task<string> ExportToGPXAsync(string imei, DateTime localStart, DateTime localEnd, int memberTimezone);
    Task<string> ExportToKMLAsync(string imei, DateTime localStart, DateTime localEnd, int memberTimezone);
    Task<OperationResult> DeleteHistoryAsync(string imei, DateTime localStart, DateTime localEnd, int memberTimezone);
    Task<IEnumerable<AlertLog>> GetAlertHistoryAsync(string imei, DateTime localStart, DateTime localEnd, int memberTimezone);
    Task<IEnumerable<DailyHistorySummary>> GetDailySummaryAsync(string imei, DateTime localEnd, int memberTimezone, int days = 7);
}
