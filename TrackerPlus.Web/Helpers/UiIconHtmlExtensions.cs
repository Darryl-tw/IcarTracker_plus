using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TrackerPlus.Web.Helpers;

public static class UiIconHtmlExtensions
{
    public static IHtmlContent UiIcon(this IHtmlHelper html, string name, int size = 16, string? extraClass = null)
        => new HtmlString(UiIconHelper.Render(name, size, extraClass));
}
