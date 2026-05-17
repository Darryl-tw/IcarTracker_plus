using TrackerPlus.Core.Common;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Services;

public interface IGeofenceService
{
    Task<IEnumerable<Geofence>> GetGeofencesAsync(int trackerTbKey);
    Task<Geofence?> GetGeofenceAsync(int trackerTbKey, int fenceIndex);
    Task<OperationResult> SaveGeofenceAsync(Geofence geofence);
    Task<OperationResult> DeleteGeofenceAsync(int trackerTbKey, int fenceIndex);
    Task<OperationResult> SetGeofenceEnableAsync(int trackerTbKey, int fenceIndex, bool enable);
    Task<bool> CheckInGeofenceAsync(double lat, double lng, double fenceLat, double fenceLng, double radius);
}
