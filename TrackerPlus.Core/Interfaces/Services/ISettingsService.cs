using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Services;

public interface ISettingsService
{
    Task<IEnumerable<(int TbKey, string GmtCode)>> GetTimeZonesAsync();
    Task<int> GetMemberTimeZoneTbKeyAsync(int memberTbKey);
    Task<OperationResult> UpdateSystemParamsAsync(int memberTbKey, string userUnit, int timeZoneTbKey);

    Task<IEnumerable<UdField>> GetUdFieldsAsync(int memberTbKey);
    Task<IEnumerable<UdLabel>> GetUdLabelsAsync(int memberTbKey);
    Task<OperationResult> SaveLabelGroupAsync(int memberTbKey,
        IEnumerable<(int TbKey, string FName)> fields,
        IEnumerable<(int TbKey, string LabelName)> labels);

    Task<ReportFieldsDto> GetReportFieldsAsync(int memberTbKey);
    Task<OperationResult> SaveReportFieldsAsync(int memberTbKey, ReportFieldsDto dto);
}
