using System.Security.Claims;
using AIVES.Business.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AIVES.Ass2.Razor.Infrastructure;

public abstract class AppPageModel : PageModel
{
    protected int CurrentUserId => User.GetAccountId();

    protected void FlashSuccess(string message) => TempData["Success"] = message;

    protected void FlashError(string message) => TempData["Error"] = message;

    protected IActionResult? MapFailure(ServiceResult result) => result.ErrorType switch
    {
        ServiceErrorType.NotFound => NotFound(),
        ServiceErrorType.Forbidden => Forbid(),
        _ => null
    };

    public override async Task OnPageHandlerExecutionAsync(
        PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        if (User.Identity?.IsAuthenticated != true ||
            !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) || id <= 0)
        {
            context.Result = Challenge();
            return;
        }

        await next();
    }
}
