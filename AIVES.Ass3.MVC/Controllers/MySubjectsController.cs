using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVES.Ass3.MVC.Controllers;

// TV5 - Ass3 - việc 5.2: Môn của tôi (chỉ Giảng viên)
[Authorize(Roles = Roles.Lecturer)]
public class MySubjectsController : AppController
{
    private readonly ISubjectService _subjects;

    public MySubjectsController(ISubjectService subjects)
    {
        _subjects = subjects;
    }

    public async Task<IActionResult> Index() => View(await _subjects.GetByLecturerAsync(CurrentUserId));
}
