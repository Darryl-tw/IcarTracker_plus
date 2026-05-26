namespace TrackerPlus.Repository.Mapping;

/// <summary>對應 TK_MSP570.dbo.Tracker + Tracker_Info 查詢結果。</summary>
internal sealed class TrackerDbRow
{
    public int TbKey { get; set; }
    public string IMEICode { get; set; } = string.Empty;
    public int Member_tbKey { get; set; }
    public string CName { get; set; } = string.Empty;
    public string TrackerEnabled { get; set; } = "Y";
    public bool PowerSavingMode { get; set; }
    public string SOSTel1 { get; set; } = string.Empty;
    public string? CMemo { get; set; }
    public string? IconFile { get; set; }
    public string? FWVersion { get; set; }
    public DateTime? TrackerVerifyExpireDate { get; set; }
    public DateTime? LastReportDate { get; set; }
    public DateTime? LastConnectedDate { get; set; }
    public DateTime? Lastlogintime { get; set; }
    public string? Lat { get; set; }
    public string? LatPos { get; set; }
    public string? Lng { get; set; }
    public string? LngPos { get; set; }
    public string? Speed { get; set; }
    public string? Direction { get; set; }
    public string? QTY_GPS { get; set; }
    public string? SatelliteStatus { get; set; }
    public string? OtherStatus { get; set; }
    public DateTime? GPSDateTime { get; set; }
    public string? FC_Lat { get; set; }
    public string? FC_Lng { get; set; }
    public string? FC_R { get; set; }
    public string? isFCEnable { get; set; }
    public string? FC_Lat1 { get; set; }
    public string? FC_Lng1 { get; set; }
    public string? FC_R1 { get; set; }
    public string? isFCEnable1 { get; set; }
    public string? FC_Lat2 { get; set; }
    public string? FC_Lng2 { get; set; }
    public string? FC_R2 { get; set; }
    public string? isFCEnable2 { get; set; }
    public string? FC_Lat3 { get; set; }
    public string? FC_Lng3 { get; set; }
    public string? FC_R3 { get; set; }
    public string? isFCEnable3 { get; set; }
    public string? MemberName { get; set; }
    public string? OBMName { get; set; }
    public string? MemberAccount { get; set; }
    public string? ICCID { get; set; }
    public string? APN { get; set; }
    public DateTime? CDate { get; set; }
    /// <summary>0=標準, 1=專業 (from PayLog.ValueAddedWeb)</summary>
    public int ValueAddedWeb { get; set; }
    /// <summary>目前有效訂單的服務到期日（vPayLog / PayLog.EDate）。</summary>
    public DateTime? PayLogEndDate { get; set; }
    /// <summary>有效天數（使用中剩餘 + 未開始訂單）；無訂單時 null。</summary>
    public int? EffectiveDays { get; set; }
    /// <summary>連線狀態（Y=連線中）</summary>
    public string? CurrentStatus { get; set; }
    /// <summary>省電/離線狀態（Y=離線中）</summary>
    public string? IsSleep { get; set; }
}
