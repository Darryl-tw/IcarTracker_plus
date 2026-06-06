using Dapper;
using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;
using TrackerPlus.Repository.Infrastructure;

namespace TrackerPlus.Services.Services;

public class SettingsService : ISettingsService
{
    private readonly IDbConnectionFactory _db;
    private readonly IMemberRepository _memberRepo;
    private readonly IUdFieldRepository _udFieldRepo;
    private readonly ILabelRepository _labelRepo;
    private readonly IReportFieldsRepository _reportRepo;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(
        IDbConnectionFactory db,
        IMemberRepository memberRepo,
        IUdFieldRepository udFieldRepo,
        ILabelRepository labelRepo,
        IReportFieldsRepository reportRepo,
        ILogger<SettingsService> logger)
    {
        _db = db;
        _memberRepo = memberRepo;
        _udFieldRepo = udFieldRepo;
        _labelRepo = labelRepo;
        _reportRepo = reportRepo;
        _logger = logger;
    }

    public async Task<IEnumerable<(int TbKey, string GmtCode)>> GetTimeZonesAsync()
    {
        const string sql = "SELECT tbKey, RTRIM(GMTCode) AS GMTCode FROM dbo.TimeZone ORDER BY tbKey";
        using var conn = _db.CreateMainConnection();
        var rows = await conn.QueryAsync(sql);
        return rows.Select(r => ((int)r.tbKey, (string)r.GMTCode)).ToList();
    }

    public Task<int> GetMemberTimeZoneTbKeyAsync(int memberTbKey)
        => _memberRepo.GetTimeZoneTbKeyAsync(memberTbKey);

    public async Task<OperationResult> UpdateSystemParamsAsync(int memberTbKey, string userUnit, int timeZoneTbKey)
    {
        try
        {
            var ok = await _memberRepo.UpdateSystemParamsAsync(memberTbKey, userUnit, timeZoneTbKey);
            return ok ? OperationResult.Ok("系統參數已儲存") : OperationResult.Fail("更新失敗");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateSystemParams 失敗 tbKey={TbKey}", memberTbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<IEnumerable<UdField>> GetUdFieldsAsync(int memberTbKey)
    {
        await _udFieldRepo.EnsureDefaultsAsync(memberTbKey);
        return await _udFieldRepo.GetByMemberAsync(memberTbKey);
    }

    public async Task<IEnumerable<UdLabel>> GetUdLabelsAsync(int memberTbKey)
    {
        var labels = (await _labelRepo.GetLabelsByMemberAsync(memberTbKey)).ToList();
        while (labels.Count < 3)
        {
            var newKey = await _labelRepo.CreateLabelAsync(memberTbKey, string.Empty);
            labels.Add(new UdLabel { TbKey = newKey, MemberTbKey = memberTbKey, LabelName = string.Empty });
        }
        return labels;
    }

    public async Task<OperationResult> SaveLabelGroupAsync(int memberTbKey,
        IEnumerable<(int TbKey, string FName)> fields,
        IEnumerable<(int TbKey, string LabelName)> labels)
    {
        try
        {
            foreach (var (tbKey, fName) in fields)
                await _udFieldRepo.UpdateAsync(tbKey, memberTbKey, fName);
            foreach (var (tbKey, labelName) in labels)
                await _labelRepo.UpdateLabelNameAsync(tbKey, memberTbKey, labelName);
            return OperationResult.Ok("已儲存");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveLabelGroup 失敗 tbKey={TbKey}", memberTbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<ReportFieldsDto> GetReportFieldsAsync(int memberTbKey)
    {
        var dto = await _reportRepo.GetByMemberAsync(memberTbKey);
        if (dto != null) return dto;
        var empty = new ReportFieldsDto();
        await _reportRepo.UpsertAsync(memberTbKey, empty);
        return empty;
    }

    public async Task<OperationResult> SaveReportFieldsAsync(int memberTbKey, ReportFieldsDto dto)
    {
        try
        {
            await _reportRepo.UpsertAsync(memberTbKey, dto);
            return OperationResult.Ok("已儲存");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveReportFields 失敗 tbKey={TbKey}", memberTbKey);
            return OperationResult.Fail(ex.Message);
        }
    }
}
