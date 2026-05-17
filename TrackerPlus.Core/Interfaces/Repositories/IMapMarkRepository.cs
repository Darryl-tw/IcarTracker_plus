using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Repositories;

public interface IMapMarkRepository
{
    Task<IEnumerable<MapMark>> GetByMemberAsync(int memberTbKey);
    Task<int> GetCountByMemberAsync(int memberTbKey);
    Task<int> CreateAsync(int memberTbKey, string address, double lat, double lng, string memo);
    Task<bool> DeleteAsync(int tbKey, int memberTbKey);
}
