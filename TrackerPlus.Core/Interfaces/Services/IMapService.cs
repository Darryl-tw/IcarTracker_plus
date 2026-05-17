using TrackerPlus.Core.Models;

namespace TrackerPlus.Core.Interfaces.Services;

public interface IMapService
{
    Task<string> ReverseGeocodeAsync(double lat, double lng);
    Task<(double Lat, double Lng)?> GeocodeAsync(string address);
    Task<(double Lat, double Lng)?> GetLocationByLBSAsync(int mcc, int mnc, int lac, int cellId);
    Task<(double Lat, double Lng)?> GetLocationByWifiAsync(IEnumerable<string> bssidList);
    string GetGoogleMapsJSApiKey();
}
