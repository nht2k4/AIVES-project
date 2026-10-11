using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVES.Ass2.Razor.Pages.Subjects;

[Authorize(Roles = Roles.Admin)]
public class DetailsModel(ISubjectService subjects, IAssignmentService assignments) : TV5PageModel
{
    public SubjectDetailDto Detail { get; private set; } = null!;
    [BindProperty] public int? LecturerId { get; set; }

    public async Task<IActionResult> OnGetAsync(int id) => await LoadAsync(id) ?? Page();

    public async Task<IActionResult> OnPostAssignAsync(int id)
    {
        var failure = await LoadAsync(id);
        if (failure is not null) return failure;
        if (LecturerId is null) ModelState.AddModelError(nameof(LecturerId), "Chọn giảng viên cần phân công.");
        if (!ModelState.IsValid) return Page();

        var result = await assignments.AssignAsync(LecturerId!.Value, id);
        if (!result.Succeeded)
        {
            failure = MapFailure(result);
            if (failure is not null) return failure;
            ModelState.AddModelError(string.Empty, result.Error!);
            return Page();
        }
        FlashSuccess("Đã phân công giảng viên.");
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUnassignAsync(int id, int lecturerId)
    {
        var failure = await LoadAsync(id);
        if (failure is not null) return failure;
        if (!ModelState.IsValid) return Page();
        var result = await assignments.UnassignAsync(lecturerId, id);
        if (!result.Succeeded)
        {
            failure = MapFailure(result);
            if (failure is not null) return failure;
            ModelState.AddModelError(string.Empty, result.Error!);
            return Page();
        }
        FlashSuccess("Đã gỡ phân công.");
        return RedirectToPage(new { id });
    }

    private async Task<IActionResult?> LoadAsync(int id)
    {
        var result = await subjects.GetDetailAsync(id);
        if (!result.Succeeded) return MapFailure(result) ?? BadRequest();
        Detail = result.Data!;
        return null;
    }
}
