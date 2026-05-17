using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Services;

public interface IIMEIService
{
    Task<IMEIDevice?> GetIMEIAsync(int tbKey);
    Task<IMEIDevice?> GetIMEIByCodeAsync(string imei);
    Task<PagedResult<IMEIDevice>> GetIMEIsPagedAsync(QueryFilter filter);
    Task<OperationResult> RegisterIMEIAsync(IMEIDevice device);
    Task<OperationResult> UpdateIMEIAsync(IMEIDevice device);
    Task<OperationResult> DeleteIMEIAsync(int tbKey);
    Task<OperationResult> BatchDeleteIMEIAsync(IEnumerable<string> imeiList);
    Task<OperationResult> MoveIMEIAsync(string imei, int newMemberTbKey, int operatorTbKey);
    Task<OperationResult> ResetIMEIAsync(string imei);
    Task<OperationResult> FullResetIMEIAsync(string imei, IMEIFullResetOptions options);
    Task<bool> ValidateIMEIFormatAsync(string imei);
    Task<int> GetIMEICountAsync(QueryFilter filter);
}
