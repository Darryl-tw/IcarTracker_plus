using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Repositories;

public interface IReportFieldsRepository
{
    Task<ReportFieldsDto?> GetByMemberAsync(int memberTbKey);
    Task<bool> UpsertAsync(int memberTbKey, ReportFieldsDto dto);
}
