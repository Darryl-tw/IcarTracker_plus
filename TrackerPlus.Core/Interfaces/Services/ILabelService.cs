using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Services;

public interface ILabelService
{
    Task<IEnumerable<UdLabel>> GetMemberLabelsAsync(int memberTbKey);
    Task<IEnumerable<UdLabel>> GetTrackerLabelsAsync(int trackerTbKey, int memberTbKey);
    Task<IEnumerable<int>> GetAssignedLabelTbKeysAsync(int trackerTbKey);
    Task<OperationResult> SetTrackerLabelsAsync(int trackerTbKey, int memberTbKey, IEnumerable<int> labelTbKeys);
    Task<OperationResult> CreateLabelAsync(int memberTbKey, string labelName);
    Task<OperationResult> DeleteLabelAsync(int labelTbKey, int memberTbKey);
}
