using AIVES.Ass2.Razor.Infrastructure;
using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace AIVES.Ass2.Razor.Pages.Exams;

public class DetailsModel(IExamService exams, IHubContext<LiveHub> live) : AppPageModel
{
    public ExamDetailDto Exam { get; private set; } = null!;
    public bool CanReallocate => Exam.Participants.All(p => p.Status == ParticipantStatuses.Scheduled);

    public async Task<IActionResult> OnGetAsync(int id) => await LoadAsync(id) ?? Page();

    public async Task<IActionResult> OnPostSetOpenAsync(int id, bool? open)
    {
        var failure = await LoadAsync(id);
        if (failure is not null) return failure;
        if (open is null) ModelState.AddModelError(string.Empty, "Chọn trạng thái phiên thi.");
        if (!ModelState.IsValid) return Page();

        var result = await exams.SetOpenAsync(CurrentUserId, id, open!.Value);
        if (!result.Succeeded) return await ShowFailureAsync(id, result);

        var message = open.Value ? "Đã mở phiên thi." : "Đã đóng phiên thi.";
        await live.NotifyAsync(message, LiveTopics.Exams, LiveTopics.Exam(id));
        FlashSuccess(message);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReallocateAsync(int id)
    {
        var failure = await LoadAsync(id);
        if (failure is not null) return failure;
        if (!ModelState.IsValid) return Page();

        // Service kiểm tra lại trạng thái thí sinh, kể cả khi trang đang mở đã cũ.
        var result = await exams.ReallocateAsync(CurrentUserId, id);
        if (!result.Succeeded) return await ShowFailureAsync(id, result);

        const string message = "Đã chọn lại bộ câu hỏi.";
        await live.NotifyAsync(message, LiveTopics.Exam(id));
        FlashSuccess(message);
        return RedirectToPage(new { id });
    }

    private async Task<IActionResult?> LoadAsync(int id)
    {
        var result = await exams.GetDetailAsync(CurrentUserId, id);
        if (!result.Succeeded) return MapFailure(result) ?? BadRequest();
        Exam = result.Data!;
        return null;
    }

    private async Task<IActionResult> ShowFailureAsync(int id, ServiceResult result)
    {
        var failure = MapFailure(result);
        if (failure is not null) return failure;
        ModelState.AddModelError(string.Empty, result.Error!);
        return await LoadAsync(id) ?? Page();
    }
}
