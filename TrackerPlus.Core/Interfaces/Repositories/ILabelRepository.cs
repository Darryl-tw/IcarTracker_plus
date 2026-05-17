using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Repositories;

public interface ILabelRepository
{
    Task<IEnumerable<UdLabel>> GetLabelsByMemberAsync(int memberTbKey);
    Task<IEnumerable<UdLabel>> GetLabelsByTrackerAsync(int trackerTbKey, int memberTbKey);
    Task<IEnumerable<int>> GetAssignedLabelTbKeysAsync(int trackerTbKey);
    Task<bool> SetTrackerLabelsAsync(int trackerTbKey, IEnumerable<int> labelTbKeys);
    Task<int> CreateLabelAsync(int memberTbKey, string labelName);
    Task<bool> DeleteLabelAsync(int labelTbKey, int memberTbKey);
    Task<string> GetLabelNamesDisplayAsync(int trackerTbKey, int memberTbKey);
}
