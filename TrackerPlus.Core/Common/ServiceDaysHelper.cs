namespace TrackerPlus.Core.Common;

/// <summary>
/// 服務有效天數（對齊舊系統 TrackerFUN.GetTrackerEffectiveDays）：
/// 使用中訂單 DATEDIFF(day,今天,EDate)+1，加上尚未開始訂單 SUM(DATEDIFF(day,SDate,EDate)+1)。
/// </summary>
public static class ServiceDaysHelper
{
    /// <summary>由 Tracker.EffectiveDays（Repository SQL 已含未開始訂單）取得列表用有效天數。</summary>
    public static int? EffectiveDays(int? effectiveDaysFromDb) => effectiveDaysFromDb;

    /// <summary>僅依單一到期日估算剩餘天數（詳情頁顯示用，不含待生效訂單）。</summary>
    public static int? RemainingDays(DateTime? serviceEndDate)
    {
        if (!serviceEndDate.HasValue) return null;
        return Math.Max(0, (int)(serviceEndDate.Value.Date - DateTime.Today).TotalDays + 1);
    }
}
