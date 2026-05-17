using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Services.Services;

public class WebServiceIPService : IWebServiceIPService
{
    private readonly IWebServiceIPRepository _ipRepo;
    private readonly ILogger<WebServiceIPService> _logger;

    public WebServiceIPService(IWebServiceIPRepository ipRepo, ILogger<WebServiceIPService> logger)
    {
        _ipRepo = ipRepo;
        _logger = logger;
    }

    public async Task<IEnumerable<WebServiceIP>> GetAllAsync()
        => await _ipRepo.GetAllAsync();

    public async Task<PagedResult<WebServiceIP>> GetPagedAsync(QueryFilter filter)
        => await _ipRepo.GetPagedAsync(filter);

    public async Task<OperationResult> CreateAsync(WebServiceIP ip)
    {
        try
        {
            ip.CDate = DateTime.UtcNow;
            ip.Status = "Y";
            var newKey = await _ipRepo.CreateAsync(ip);
            return OperationResult.Ok("新增成功", newKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "新增 WebServiceIP 失敗 {IP}", ip.IPAddress);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteAsync(int tbKey)
    {
        try
        {
            var ok = await _ipRepo.DeleteAsync(tbKey);
            return ok ? OperationResult.Ok("刪除成功") : OperationResult.Fail("找不到記錄");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除 WebServiceIP 失敗 TbKey={TbKey}", tbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<bool> IsAllowedAsync(string ipAddress)
        => await _ipRepo.IsAllowedAsync(ipAddress);
}
