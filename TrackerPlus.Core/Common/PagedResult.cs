namespace TrackerPlus.Core.Common;

/// <summary>通用分頁結果</summary>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;
}

/// <summary>通用操作結果</summary>
public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public string ErrorCode { get; set; } = string.Empty;

    public static OperationResult Ok(string message = "Success", object? data = null)
        => new() { Success = true, Message = message, Data = data };

    public static OperationResult Fail(string message, string errorCode = "")
        => new() { Success = false, Message = message, ErrorCode = errorCode };
}

/// <summary>查詢過濾條件基底</summary>
public class QueryFilter
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 100;
    public string? Keyword { get; set; }
    /// <summary>經銷商（OBM 名稱 / Domain / Userdb.UserName）</summary>
    public string? Obm { get; set; }
    /// <summary>IMEI 或裝置識別名稱</summary>
    public string? Imei { get; set; }
    /// <summary>會員帳號 / Email / 姓名</summary>
    public string? Account { get; set; }
    public string? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string SortBy { get; set; } = "TbKey";
    public bool SortDesc { get; set; } = true;
}
