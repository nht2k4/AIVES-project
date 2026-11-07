using AIVES.Ass3.MVC.ViewModels;
using AIVES.Business.Common;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVES.Ass3.MVC.Controllers;

// TV5 - Ass3 - việc 5.2: Môn học và phân công (chỉ Admin)
[Authorize(Roles = Roles.Admin)]
public class SubjectsController : AppController
{
    private readonly ISubjectService _subjects;
    private readonly IAssignmentService _assignments;

    public SubjectsController(ISubjectService subjects, IAssignmentService assignments)
    {
        _subjects = subjects;
        _assignments = assignments;
    }

    public async Task<IActionResult> Index() => View(await _subjects.GetAllAsync());

    public async Task<IActionResult> Details(int id)
    {
        var model = new SubjectDetailsViewModel();
        return await LoadAsync(id, model) ?? View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int id, SubjectDetailsViewModel model)
    {
        var failure = await LoadAsync(id, model);
        if (failure is not null) return failure;

        if (model.LecturerId is null)
            ModelState.AddModelError(nameof(model.LecturerId), "Chọn giảng viên cần phân công.");
        if (!ModelState.IsValid) return View(nameof(Details), model);

        var result = await _assignments.AssignAsync(model.LecturerId!.Value, id);
        if (!result.Succeeded) return ShowError(result, model);

        FlashSuccess("Đã phân công giảng viên.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unassign(int id, int lecturerId)
    {
        var model = new SubjectDetailsViewModel();
        var failure = await LoadAsync(id, model);
        if (failure is not null) return failure;

        var result = await _assignments.UnassignAsync(lecturerId, id);
        if (!result.Succeeded) return ShowError(result, model);

        FlashSuccess("Đã gỡ phân công.");
        return RedirectToAction(nameof(Details), new { id });
    }

    // Nạp chi tiết môn để hiển thị lại trang (kể cả khi form lỗi)
    private async Task<IActionResult?> LoadAsync(int id, SubjectDetailsViewModel model)
    {
        var result = await _subjects.GetDetailAsync(id);
        if (!result.Succeeded) return MapFailure(result) ?? BadRequest();
        model.Detail = result.Data!;
        return null;
    }

    private IActionResult ShowError(ServiceResult result, SubjectDetailsViewModel model)
    {
        var failure = MapFailure(result);
        if (failure is not null) return failure;
        ModelState.AddModelError(string.Empty, result.Error!);
        return View(nameof(Details), model);
    }
}
