using Dapper;
using Microsoft.Extensions.Logging;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Models;
using TrackerPlus.Repository.Infrastructure;

namespace TrackerPlus.Repository.Repositories;

// Geofence data is stored in Tracker table columns FC0~FC3
public class GeofenceRepository : IGeofenceRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<GeofenceRepository> _logger;

    public GeofenceRepository(IDbConnectionFactory db, ILogger<GeofenceRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<Geofence>> GetByTrackerAsync(int trackerTbKey)
    {
        const string sql = @"SELECT TbKey, IMEICODE,
            FC0_Lat, FC0_Lng, FC0_Radius, FC0_Enable,
            FC1_Lat, FC1_Lng, FC1_Radius, FC1_Enable,
            FC2_Lat, FC2_Lng, FC2_Radius, FC2_Enable,
            FC3_Lat, FC3_Lng, FC3_Radius, FC3_Enable
            FROM Tracker WHERE TbKey=@TbKey";
        using var conn = _db.CreateMainConnection();
        var row = await conn.QuerySingleOrDefaultAsync(sql, new { TbKey = trackerTbKey });
        if (row == null) return Enumerable.Empty<Geofence>();

        return BuildGeofences(trackerTbKey, row);
    }

    public async Task<Geofence?> GetByIndexAsync(int trackerTbKey, int fenceIndex)
    {
        var geofences = await GetByTrackerAsync(trackerTbKey);
        return geofences.FirstOrDefault(g => g.FenceIndex == fenceIndex);
    }

    public async Task<bool> SaveGeofenceAsync(Geofence geofence)
    {
        var idx = geofence.FenceIndex;
        var sql = $@"UPDATE Tracker SET
            FC{idx}_Lat=@Lat, FC{idx}_Lng=@Lng, FC{idx}_Radius=@Radius, FC{idx}_Enable=@Enable
            WHERE TbKey=@TbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, new
        {
            TbKey = geofence.Tracker_TbKey,
            Lat = geofence.Lat.ToString(),
            Lng = geofence.Lng.ToString(),
            geofence.Radius,
            geofence.Enable
        }) > 0;
    }

    public async Task<bool> DeleteGeofenceAsync(int trackerTbKey, int fenceIndex)
    {
        var sql = $@"UPDATE Tracker SET
            FC{fenceIndex}_Lat='', FC{fenceIndex}_Lng='', FC{fenceIndex}_Radius=0, FC{fenceIndex}_Enable='N'
            WHERE TbKey=@TbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, new { TbKey = trackerTbKey }) > 0;
    }

    public async Task<bool> DeleteAllByTrackerAsync(int trackerTbKey)
    {
        const string sql = @"UPDATE Tracker SET
            FC0_Lat='', FC0_Lng='', FC0_Radius=0, FC0_Enable='N',
            FC1_Lat='', FC1_Lng='', FC1_Radius=0, FC1_Enable='N',
            FC2_Lat='', FC2_Lng='', FC2_Radius=0, FC2_Enable='N',
            FC3_Lat='', FC3_Lng='', FC3_Radius=0, FC3_Enable='N'
            WHERE TbKey=@TbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, new { TbKey = trackerTbKey }) > 0;
    }

    public async Task<bool> SetEnableAsync(int trackerTbKey, int fenceIndex, bool enable)
    {
        var sql = $"UPDATE Tracker SET FC{fenceIndex}_Enable=@Enable WHERE TbKey=@TbKey";
        using var conn = _db.CreateMainConnection();
        return await conn.ExecuteAsync(sql, new { TbKey = trackerTbKey, Enable = enable ? "Y" : "N" }) > 0;
    }

    private static IEnumerable<Geofence> BuildGeofences(int trackerTbKey, dynamic row)
    {
        var result = new List<Geofence>();
        for (int i = 0; i < 4; i++)
        {
            result.Add(new Geofence
            {
                Tracker_TbKey = trackerTbKey,
                IMEICODE = row.IMEICODE ?? string.Empty,
                FenceIndex = i,
                Lat = TryParseDouble(GetFenceField(row, i, "Lat")),
                Lng = TryParseDouble(GetFenceField(row, i, "Lng")),
                Radius = GetFenceRadius(row, i),
                Enable = GetFenceField(row, i, "Enable") ?? "N"
            });
        }
        return result;
    }

    private static string? GetFenceField(dynamic row, int index, string field)
    {
        var dict = (IDictionary<string, object>)row;
        var key = $"FC{index}_{field}";
        return dict.TryGetValue(key, out var val) ? val?.ToString() : null;
    }

    private static double GetFenceRadius(dynamic row, int index)
    {
        var dict = (IDictionary<string, object>)row;
        var key = $"FC{index}_Radius";
        if (dict.TryGetValue(key, out var val) && val != null)
            return Convert.ToDouble(val);
        return 0;
    }

    private static double TryParseDouble(string? s)
        => double.TryParse(s, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0;
}
