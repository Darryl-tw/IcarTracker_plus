using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Services.Services;

public class OBMService : IOBMService
{
    private readonly IOBMRepository _repo;
    private readonly ILogger<OBMService> _logger;

    public OBMService(IOBMRepository repo, ILogger<OBMService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public Task<OBM?> GetOBMAsync(int tbKey) => _repo.GetByIdAsync(tbKey);

    public Task<IEnumerable<OBM>> GetAllOBMsAsync() => _repo.GetAllAsync();

    public Task<PagedResult<OBM>> GetOBMsPagedAsync(QueryFilter filter) => _repo.GetPagedAsync(filter);

    public async Task<OperationResult> CreateOBMAsync(OBM obm)
    {
        try
        {
            var key = await _repo.CreateAsync(obm);
            return OperationResult.Ok("新增成功", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "新增 OBM 失敗 {CName}", obm.CName);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> UpdateOBMAsync(OBM obm)
    {
        try
        {
            var ok = await _repo.UpdateAsync(obm);
            return ok ? OperationResult.Ok("更新成功") : OperationResult.Fail("找不到 OBM");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新 OBM 失敗 TbKey={TbKey}", obm.TbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteOBMAsync(int tbKey)
    {
        try
        {
            var ok = await _repo.DeleteAsync(tbKey);
            return ok ? OperationResult.Ok("刪除成功") : OperationResult.Fail("找不到 OBM");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除 OBM 失敗 TbKey={TbKey}", tbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<(string Title, string Logo, string Contact)> GetDomainInfoAsync(string domain)
    {
        var obm = await _repo.GetByDomainAsync(domain);
        return obm == null
            ? (string.Empty, string.Empty, string.Empty)
            : (obm.WebTitle, obm.Logo, obm.ContactPerson);
    }
}
