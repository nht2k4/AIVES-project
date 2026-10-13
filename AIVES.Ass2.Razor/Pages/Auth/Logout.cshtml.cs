using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AIVES.Ass2.Razor.Pages.Auth;

// TV4 - Ass2 - việc 4.1: đăng xuất phải là POST để không bị đăng xuất bằng GET (CSRF / prefetch).
[Authorize]
public class LogoutModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("./Login");

    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("./Login");
    }
}
