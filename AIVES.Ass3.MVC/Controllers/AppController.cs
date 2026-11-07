using AIVES.Ass3.MVC.Infrastructure;
using AIVES.Business.Common;
using Microsoft.AspNetCore.Mvc;

namespace AIVES.Ass3.MVC.Controllers;

public abstract class AppController : Controller
{
    protected int CurrentUserId => User.RequireUserId();
    protected string? CurrentUserRole => User.GetUserRole();

    protected void FlashSuccess(string message) => TempData["Success"] = message;
    protected void FlashError(string message) => TempData["Error"] = message;

    // NotFound/Forbidden -> mã HTTP tương ứng. Lỗi Validation -> null để Controller tự hiển thị lên form.
    protected IActionResult? MapFailure(ServiceResult result) => result.ErrorType switch
    {
        ServiceErrorType.NotFound => NotFound(),
        ServiceErrorType.Forbidden => Forbid(),
        _ => null
    };
}
