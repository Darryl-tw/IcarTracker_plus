using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Services.Services;

public class DealerService : IDealerService
{
    private readonly IDealerRepository _repo;
    private readonly ILogger<DealerService> _logger;

    public DealerService(IDealerRepository repo, ILogger<DealerService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<IEnumerable<Dealer>> GetDealersAsync(string? keyword, string? status)
        => await _repo.GetAllAsync(keyword, status);

    public async Task<Dealer?> GetDealerAsync(int tbKey)
        => await _repo.GetByIdAsync(tbKey);

    public async Task<OperationResult> UpdateDealerAsync(Dealer dealer)
    {
        try
        {
            var ok = await _repo.UpdateAsync(dealer);
            return ok ? OperationResult.Ok("更新成功") : OperationResult.Fail("找不到經銷商");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新經銷商失敗 TbKey={TbKey}", dealer.TbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> CreateDealerAsync(Dealer dealer)
    {
        try
        {
            var id = await _repo.CreateAsync(dealer);
            return id > 0 ? OperationResult.Ok("新增成功") : OperationResult.Fail("新增失敗");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "新增經銷商失敗");
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteDealerAsync(int tbKey)
    {
        try
        {
            var ok = await _repo.DeleteAsync(tbKey);
            return ok ? OperationResult.Ok("刪除成功") : OperationResult.Fail("找不到經銷商");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除經銷商失敗 TbKey={TbKey}", tbKey);
            return OperationResult.Fail(ex.Message);
        }
    }
}
