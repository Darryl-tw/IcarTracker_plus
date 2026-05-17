using TrackerPlus.Core.Common;

namespace TrackerPlus.Core.Interfaces.Services;

public interface IReportService
{
    Task<byte[]> GenerateMemberListExcelAsync(QueryFilter filter);
    Task<byte[]> GenerateMemberListCsvAsync(QueryFilter filter);
    Task<byte[]> GeneratePayLogExcelAsync(QueryFilter filter);
    Task<byte[]> GeneratePayLogCsvAsync(QueryFilter filter);
    Task<byte[]> GenerateTrackerListExcelAsync(QueryFilter filter);
    Task<byte[]> GenerateTrackerListCsvAsync(QueryFilter filter);
    Task<byte[]> GenerateRenewalListExcelAsync(DateTime startDate, DateTime endDate);
    Task<byte[]> GenerateRenewalListCsvAsync(DateTime startDate, DateTime endDate);
    Task<OperationResult> SendMemberListEmailAsync(string toEmail, QueryFilter filter);
    Task<OperationResult> SendOBMPayLogEmailAsync(int obmTbKey, DateTime startDate, DateTime endDate);
}
