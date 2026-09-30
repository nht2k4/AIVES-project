using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVES.WebMVC.Controllers;

[Authorize(Roles = Roles.Lecturer)]
public class MySubjectsController : AppController
{
    private readonly ISubjectService _subjectService;

    public MySubjectsController(ISubjectService subjectService) => _subjectService = subjectService;

    public async Task<IActionResult> Index() =>
        View(await _subjectService.GetByLecturerAsync(CurrentUserId));
}
