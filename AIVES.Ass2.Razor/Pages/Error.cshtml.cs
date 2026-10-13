using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AIVES.Ass2.Razor.Pages;

[AllowAnonymous]
public class ErrorModel : PageModel
{
    public void OnGet()
    {
    }
}
