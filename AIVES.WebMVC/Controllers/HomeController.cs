using AIVES.Business.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVES.WebMVC.Controllers;

public class HomeController : AppController
{
    [Authorize]
    public IActionResult Index() =>
        User.IsInRole(Roles.Admin)
            ? RedirectToAction("Index", "Subjects")
            : RedirectToAction("Index", "MySubjects");

    [AllowAnonymous]
    public IActionResult Error() => View();
}
