using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Repositories;

public interface IUdFieldRepository
{
    Task<IEnumerable<UdField>> GetByMemberAsync(int memberTbKey);
    Task EnsureDefaultsAsync(int memberTbKey);
    Task<bool> UpdateAsync(int tbKey, int memberTbKey, string fName);
}
