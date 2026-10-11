using System.ComponentModel.DataAnnotations;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVES.Ass2.Razor.Pages.Exams;

[Authorize(Roles = Roles.Admin + "," + Roles.Lecturer)]
public class QuestionsModel(IQuestionService questions, ISubjectService subjects) : TV5PageModel
{
    public SubjectDto Subject { get; private set; } = null!;
    public IReadOnlyList<QuestionDto> Questions { get; private set; } = Array.Empty<QuestionDto>();
    [BindProperty, StringLength(1000, ErrorMessage = "Nội dung câu hỏi không quá 1000 ký tự.")]
    public new string? Content { get; set; }
    [BindProperty, StringLength(1000, ErrorMessage = "Ý chính trả lời không quá 1000 ký tự.")]
    public string? ExpectedPoints { get; set; }
    public bool IsAdmin => User.IsInRole(Roles.Admin);

    public async Task<IActionResult> OnGetAsync(int subjectId) => await LoadAsync(subjectId) ?? Page();

    public async Task<IActionResult> OnPostAddAsync(int subjectId)
    {
        var failure = await LoadAsync(subjectId);
        if (failure is not null) return failure;
        if (!ModelState.IsValid) return Page();
        var result = await questions.AddAsync(CurrentUserId, new(subjectId, Content ?? string.Empty, ExpectedPoints));
        if (!result.Succeeded)
        {
            failure = MapFailure(result);
            if (failure is not null) return failure;
            ModelState.AddModelError(string.Empty, result.Error!);
            return Page();
        }
        FlashSuccess("Đã thêm câu hỏi.");
        return RedirectToPage(new { subjectId });
    }

    public async Task<IActionResult> OnPostSetActiveAsync(int subjectId, int questionId, bool? isActive)
    {
        var failure = await LoadAsync(subjectId);
        if (failure is not null) return failure;
        if (isActive is null) ModelState.AddModelError(nameof(isActive), "Chọn trạng thái câu hỏi.");
        if (!ModelState.IsValid) return Page();
        // Bind the mutation to the displayed bank, including for an admin with access to both subjects.
        if (!Questions.Any(q => q.Id == questionId)) return NotFound();
        var result = await questions.SetActiveAsync(CurrentUserId, questionId, isActive!.Value);
        if (!result.Succeeded)
        {
            failure = MapFailure(result);
            if (failure is not null) return failure;
            ModelState.AddModelError(string.Empty, result.Error!);
            return Page();
        }
        FlashSuccess(isActive.Value ? "Đã bật câu hỏi." : "Đã tắt câu hỏi.");
        return RedirectToPage(new { subjectId });
    }

    private async Task<IActionResult?> LoadAsync(int subjectId)
    {
        var result = await questions.GetBySubjectAsync(CurrentUserId, subjectId);
        if (!result.Succeeded) return MapFailure(result) ?? BadRequest();
        Questions = result.Data!;
        var detail = await subjects.GetDetailAsync(subjectId);
        if (!detail.Succeeded) return MapFailure(detail) ?? BadRequest();
        Subject = detail.Data!.Subject;
        return null;
    }
}
