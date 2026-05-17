using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Services.Services;

public class LabelService : ILabelService
{
    private readonly ILabelRepository _labelRepo;
    private readonly ILogger<LabelService> _logger;

    public LabelService(ILabelRepository labelRepo, ILogger<LabelService> logger)
    {
        _labelRepo = labelRepo;
        _logger = logger;
    }

    public Task<IEnumerable<UdLabel>> GetMemberLabelsAsync(int memberTbKey)
        => _labelRepo.GetLabelsByMemberAsync(memberTbKey);

    public Task<IEnumerable<UdLabel>> GetTrackerLabelsAsync(int trackerTbKey, int memberTbKey)
        => _labelRepo.GetLabelsByTrackerAsync(trackerTbKey, memberTbKey);

    public Task<IEnumerable<int>> GetAssignedLabelTbKeysAsync(int trackerTbKey)
        => _labelRepo.GetAssignedLabelTbKeysAsync(trackerTbKey);

    public async Task<OperationResult> SetTrackerLabelsAsync(int trackerTbKey, int memberTbKey, IEnumerable<int> labelTbKeys)
    {
        try
        {
            var memberLabels = (await _labelRepo.GetLabelsByMemberAsync(memberTbKey)).Select(l => l.TbKey).ToHashSet();
            var valid = labelTbKeys.Where(k => memberLabels.Contains(k)).ToList();
            var ok = await _labelRepo.SetTrackerLabelsAsync(trackerTbKey, valid);
            return ok ? OperationResult.Ok("標籤已更新") : OperationResult.Fail("更新失敗");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新 Tracker 標籤失敗");
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> CreateLabelAsync(int memberTbKey, string labelName)
    {
        if (string.IsNullOrWhiteSpace(labelName))
            return OperationResult.Fail("標籤名稱不可為空");
        try
        {
            var id = await _labelRepo.CreateLabelAsync(memberTbKey, labelName);
            return OperationResult.Ok("新增成功", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "新增標籤失敗");
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteLabelAsync(int labelTbKey, int memberTbKey)
    {
        try
        {
            var ok = await _labelRepo.DeleteLabelAsync(labelTbKey, memberTbKey);
            return ok ? OperationResult.Ok("刪除成功") : OperationResult.Fail("找不到標籤");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除標籤失敗");
            return OperationResult.Fail(ex.Message);
        }
    }
}
