namespace TrackerPlus.Core.Common;

/// <summary>服務有效天數計算（對齊舊系統 DXTKGrid：DATEDIFF(day, GETDATE(), EDate) + 1）。</summary>
public static class ServiceDaysHelper
{
    public static int? RemainingDays(DateTime? serviceEndDate)
    {
        if (!serviceEndDate.HasValue) return null;
        return Math.Max(0, (int)(serviceEndDate.Value.Date - DateTime.Today).TotalDays + 1);
    }
}
