using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Repositories;

public interface IGeofenceRepository
{
    Task<IEnumerable<Geofence>> GetByTrackerAsync(int trackerTbKey);
    Task<Geofence?> GetByIndexAsync(int trackerTbKey, int fenceIndex);
    Task<bool> SaveGeofenceAsync(Geofence geofence);
    Task<bool> DeleteGeofenceAsync(int trackerTbKey, int fenceIndex);
    Task<bool> DeleteAllByTrackerAsync(int trackerTbKey);
    Task<bool> SetEnableAsync(int trackerTbKey, int fenceIndex, bool enable);
}
