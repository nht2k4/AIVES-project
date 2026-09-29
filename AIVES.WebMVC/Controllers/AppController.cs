using AIVES.Business.Common;
using AIVES.WebMVC.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace AIVES.WebMVC.Controllers;

public abstract class AppController : Controller
{
    protected int CurrentUserId => User.RequireUserId();

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
