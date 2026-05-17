namespace TrackerPlus.Repository.Mapping;

internal sealed class MemberDbRow
{
    public int TbKey { get; set; }
    public int OBM_tbKey { get; set; }
    public string ID { get; set; } = string.Empty;
    public string EMail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string CName { get; set; } = string.Empty;
    public string MemberStatus { get; set; } = "N";
    public string DynaCheckCode { get; set; } = string.Empty;
    public string UserLanguage { get; set; } = "CHT";
    public string Unit { get; set; } = "K";
    public int TimeZone_tbkey { get; set; }
    public string Tel { get; set; } = string.Empty;
    public string Addr { get; set; } = string.Empty;
    public DateTime ConnectionTime { get; set; }
    public bool issendemail { get; set; }
    public bool ispush { get; set; }
    public DateTime CDate { get; set; }
    public string? GMTCode { get; set; }
}
