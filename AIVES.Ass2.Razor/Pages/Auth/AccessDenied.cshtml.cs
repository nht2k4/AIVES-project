using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AIVES.Ass2.Razor.Pages.Auth;

// TV4 - Ass2 - việc 4.1: trang đích khi cookie auth trả Forbid / AccessDenied.
[AllowAnonymous]
public class AccessDeniedModel : PageModel
{
    public void OnGet()
    {
    }
}
