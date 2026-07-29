using TrackerPlus.Core.Common;

namespace TrackerPlus.Web.Helpers;

/// <summary>Admin DBGrid 標題排序 UI 輔助。</summary>
public static class GridSortHelper
{
    public static string ThClass(QueryFilter? filter, string sortKey)
    {
        if (filter == null || string.IsNullOrWhiteSpace(filter.SortBy)
            || !string.Equals(filter.SortBy, sortKey, StringComparison.OrdinalIgnoreCase))
            return "sortable";
        return filter.SortDesc ? "sortable sort-desc" : "sortable sort-asc";
    }

    public static QueryFilter ApplySort(QueryFilter filter, string? sortBy, bool sortDesc, string defaultSort, bool defaultDesc = true)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            filter.SortBy = defaultSort;
            filter.SortDesc = defaultDesc;
        }
        else
        {
            filter.SortBy = sortBy.Trim();
            filter.SortDesc = sortDesc;
        }
        return filter;
    }
}
