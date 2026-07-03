using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using TrackerPlus.Web.Resources;

namespace TrackerPlus.Web.Controllers.Admin;

[Area("Admin")]
[Authorize(AuthenticationSchemes = "AdminCookie")]
public abstract class AdminBaseController : Controller
{
    private IStringLocalizer<SharedResources>? _localizer;
    protected IStringLocalizer<SharedResources> L =>
        _localizer ??= HttpContext.RequestServices.GetRequiredService<IStringLocalizer<SharedResources>>();

    protected string AdminAccount => User.Identity?.Name ?? string.Empty;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ViewBag.AdminAccount = AdminAccount;
        base.OnActionExecuting(context);
    }
}
