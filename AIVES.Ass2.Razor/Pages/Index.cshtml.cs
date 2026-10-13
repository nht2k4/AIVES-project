using AIVES.Business.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AIVES.Ass2.Razor.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        if (User.IsInRole(Roles.Admin))
            return RedirectToPage("/Subjects/Index");
        if (User.IsInRole(Roles.Lecturer))
            return RedirectToPage("/MySubjects/Index");
        return RedirectToPage("/Auth/Login");
    }
}
