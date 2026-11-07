using System.Security.Claims;
using AIVES.Business.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVES.Ass3.MVC.Controllers.Api;

[ApiController]
[Authorize]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected int CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    protected ActionResult Respond(ServiceResult result) => result.ErrorType switch
    {
        ServiceErrorType.None => NoContent(),
        ServiceErrorType.NotFound => NotFound(new { error = result.Error }),
        ServiceErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error }),
        _ => BadRequest(new { error = result.Error })
    };

    protected ActionResult Respond<T>(ServiceResult<T> result) => result.ErrorType switch
    {
        ServiceErrorType.None => Ok(result.Data),
        ServiceErrorType.NotFound => NotFound(new { error = result.Error }),
        ServiceErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error }),
        _ => BadRequest(new { error = result.Error })
    };
}
