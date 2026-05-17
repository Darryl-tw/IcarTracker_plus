using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TrackerPlus.Web.Controllers.Admin;

[Area("Admin")]
[Authorize(AuthenticationSchemes = "AdminCookie")]
public abstract class AdminBaseController : Controller
{
    protected string AdminAccount => User.Identity?.Name ?? string.Empty;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ViewBag.AdminAccount = AdminAccount;
        base.OnActionExecuting(context);
    }
}
