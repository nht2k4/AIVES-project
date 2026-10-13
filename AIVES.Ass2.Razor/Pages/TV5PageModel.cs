using System.Security.Claims;
using AIVES.Business.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AIVES.Ass2.Razor.Pages;

// Narrow helper for TV5 pages, independent of TV6's pending AppPageModel.
// TV6 can later consolidate these helpers into AppPageModel without changing page routes/contracts.
public abstract class TV5PageModel : PageModel
{
    protected int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    protected void FlashSuccess(string message) => TempData["Success"] = message;

    protected IActionResult? MapFailure(ServiceResult result) => result.ErrorType switch
    {
        ServiceErrorType.NotFound => NotFound(),
        ServiceErrorType.Forbidden => Forbid(),
        _ => null
    };

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
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
