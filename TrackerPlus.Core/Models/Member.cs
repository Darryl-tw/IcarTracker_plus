namespace TrackerPlus.Core.Models;

/// <summary>會員資料模型</summary>
public class Member
{
    public int TbKey { get; set; }
    public int OBM_TbKey { get; set; }
    public string ID { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string CName { get; set; } = string.Empty;
    /// <summary>Y=啟用, D=停用, S=暫停, N=新建</summary>
    public string MemberStatus { get; set; } = "N";
    public string DynaCheckCode { get; set; } = string.Empty;
    /// <summary>CHT=繁體中文, CHS=簡體中文, ENG=英文</summary>
    public string UserLanguage { get; set; } = "CHT";
    /// <summary>1=Traceez帳號, 2=IMEI Code, 3=Gmail</summary>
    public int UserType { get; set; }
    /// <summary>單位 K=公里, M=英里</summary>
    public string UserUnit { get; set; } = "K";
    /// <summary>時區偏移(分鐘)，台灣=480</summary>
    public int Timezoom { get; set; } = 480;
    public string Tel { get; set; } = string.Empty;
    public string Addr { get; set; } = string.Empty;
    public DateTime ConnectionTime { get; set; }
    public bool IsSendEmail { get; set; }
    public bool IsPush { get; set; }
    public DateTime CDate { get; set; } = DateTime.UtcNow;
}
