using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using AIVES.WebMVC.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVES.WebMVC.Controllers;

[Authorize(Roles = Roles.Admin)]
public class SubjectsController : AppController
{
    private readonly ISubjectService _subjectService;
    private readonly IAssignmentService _assignmentService;

    public SubjectsController(ISubjectService subjectService, IAssignmentService assignmentService)
    {
        _subjectService = subjectService;
        _assignmentService = assignmentService;
    }

    public async Task<IActionResult> Index() => View(await _subjectService.GetAllAsync());

    public async Task<IActionResult> Details(int id)
    {
        var result = await _subjectService.GetDetailAsync(id);
        if (!result.Succeeded) return MapFailure(result) ?? NotFound();

        return View(new SubjectDetailsViewModel
        {
            Detail = result.Data!,
            Assign = new AssignLecturerViewModel { SubjectId = id }
        });
    }

    // MAIN FLOW: Controller chỉ nhận request -> gọi Service -> trả kết quả. Không có luật nghiệp vụ ở đây.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign([Bind(Prefix = "Assign")] AssignLecturerViewModel model)
    {
        if (!ModelState.IsValid || model.LecturerId is null)
        {
            FlashError("Chọn giảng viên cần phân công.");
            return RedirectToAction(nameof(Details), new { id = model.SubjectId });
        }

        var result = await _assignmentService.AssignAsync(model.LecturerId.Value, model.SubjectId);

        if (result.Succeeded) FlashSuccess("Đã phân công giảng viên.");
        else FlashError(result.Error!);

        return RedirectToAction(nameof(Details), new { id = model.SubjectId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Unassign(int subjectId, int lecturerId)
    {
        var result = await _assignmentService.UnassignAsync(lecturerId, subjectId);

        if (result.Succeeded) FlashSuccess("Đã gỡ phân công.");
        else FlashError(result.Error!);

        return RedirectToAction(nameof(Details), new { id = subjectId });
    }
}
