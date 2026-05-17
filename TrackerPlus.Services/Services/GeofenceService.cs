using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Common;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Core.Models;

namespace TrackerPlus.Services.Services;

public class GeofenceService : IGeofenceService
{
    private readonly IGeofenceRepository _geofenceRepo;
    private readonly ILogger<GeofenceService> _logger;

    public GeofenceService(IGeofenceRepository geofenceRepo, ILogger<GeofenceService> logger)
    {
        _geofenceRepo = geofenceRepo;
        _logger = logger;
    }

    public async Task<IEnumerable<Geofence>> GetGeofencesAsync(int trackerTbKey)
        => await _geofenceRepo.GetByTrackerAsync(trackerTbKey);

    public async Task<Geofence?> GetGeofenceAsync(int trackerTbKey, int fenceIndex)
        => await _geofenceRepo.GetByIndexAsync(trackerTbKey, fenceIndex);

    public async Task<OperationResult> SaveGeofenceAsync(Geofence geofence)
    {
        try
        {
            var ok = await _geofenceRepo.SaveGeofenceAsync(geofence);
            return ok ? OperationResult.Ok("儲存成功") : OperationResult.Fail("儲存失敗");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "儲存電子柵欄失敗 TrackerTbKey={Key}", geofence.Tracker_TbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> DeleteGeofenceAsync(int trackerTbKey, int fenceIndex)
    {
        try
        {
            var ok = await _geofenceRepo.DeleteGeofenceAsync(trackerTbKey, fenceIndex);
            return ok ? OperationResult.Ok("刪除成功") : OperationResult.Fail("刪除失敗");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除電子柵欄失敗 TrackerTbKey={Key} Index={Idx}", trackerTbKey, fenceIndex);
            return OperationResult.Fail(ex.Message);
        }
    }

    public async Task<OperationResult> SetGeofenceEnableAsync(int trackerTbKey, int fenceIndex, bool enable)
    {
        try
        {
            var ok = await _geofenceRepo.SetEnableAsync(trackerTbKey, fenceIndex, enable);
            return ok ? OperationResult.Ok("設定成功") : OperationResult.Fail("設定失敗");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "設定電子柵欄啟用失敗 TrackerTbKey={Key}", trackerTbKey);
            return OperationResult.Fail(ex.Message);
        }
    }

    public Task<bool> CheckInGeofenceAsync(double lat, double lng, double fenceLat, double fenceLng, double radius)
    {
        var distance = HaversineDistanceMeters(lat, lng, fenceLat, fenceLng);
        return Task.FromResult(distance <= radius);
    }

    // Haversine formula: returns distance in meters
    private static double HaversineDistanceMeters(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371000;
        var dLat = ToRad(lat2 - lat1);
        var dLng = ToRad(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;
}
