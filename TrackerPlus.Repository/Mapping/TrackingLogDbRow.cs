namespace TrackerPlus.Repository.Mapping;

internal sealed class TrackingLogDbRow
{
    public string IMEICode { get; set; } = string.Empty;
    public DateTime UTCTime { get; set; }
    public string? Lat { get; set; }
    public string? LatPos { get; set; }
    public string? Lng { get; set; }
    public string? LngPos { get; set; }
    public string? Speed { get; set; }
    public string? Direction { get; set; }
    public string? Distance { get; set; }
    public string? OtherStatus { get; set; }
    public string? QTY_GPS { get; set; }
    public string? SatelliteStatus { get; set; }
    public string? HDOP { get; set; }
    public string Type { get; set; } = "G";
}
