namespace TrackerPlus.Core.Common;

/// <summary>系統設定強型別</summary>
public class AppSettings
{
    public string WebDomain { get; set; } = string.Empty;
    public string WebSiteTitle { get; set; } = "GPS Tracker";
    public string Serial { get; set; } = string.Empty;
    public string Language { get; set; } = "CHT";
    public int ServerTimeZone { get; set; } = 480;
    public string CompanyID { get; set; } = "002";
    public string SystemID { get; set; } = "00001";
    public string SystemName { get; set; } = "iCar";
    public int PageSize { get; set; } = 100;
    public int MaxMultiWindows { get; set; } = 6;
    public GoogleApiSettings GoogleApi { get; set; } = new();
    public OAuthSettings OAuth { get; set; } = new();
    public EmailSettings Email { get; set; } = new();
}

public class GoogleApiSettings
{
    public string DeveloperUse { get; set; } = string.Empty;
    public string URL { get; set; } = string.Empty;
    public string JS { get; set; } = string.Empty;
    public string LBS { get; set; } = string.Empty;
}

public class OAuthSettings
{
    public string GmailClientId { get; set; } = string.Empty;
    public string GmailKey { get; set; } = string.Empty;
    public string AppleClientId { get; set; } = string.Empty;
    public string AppleRedirectURL { get; set; } = string.Empty;
}

public class EmailSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 25;
    public string SmtpPassword { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
}

/// <summary>資料庫連線設定</summary>
public class DatabaseSettings
{
    public string MainConnection { get; set; } = string.Empty;
    public string LogConnection { get; set; } = string.Empty;
    public string CellularBaseConnection { get; set; } = string.Empty;
    public string ChinaOffsetConnection { get; set; } = string.Empty;
    public string AddressConnection { get; set; } = string.Empty;
}
