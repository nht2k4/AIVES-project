using AIVES.Business.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVES.Ass3.MVC.Controllers;

[Authorize]
public class HomeController : AppController
{
    public IActionResult Index()
    {
        if (User.IsInRole(Roles.Student))
        {
            return RedirectToAction("Index", "MyExams");
        }

        return RedirectToAction("Index", "Exams");
    }

    [AllowAnonymous]
    public IActionResult Error()
    {
        return View();
    }
}
