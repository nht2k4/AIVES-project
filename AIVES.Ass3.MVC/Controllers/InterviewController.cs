using AIVES.Ass3.MVC.Infrastructure;
using AIVES.Business.Common;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace AIVES.Ass3.MVC.Controllers;

[Authorize]
public class InterviewController : AppController
{
    private readonly IInterviewService _interviewService;
    private readonly IHubContext<LiveHub> _hubContext;

    public InterviewController(IInterviewService interviewService, IHubContext<LiveHub> hubContext)
    {
        _interviewService = interviewService;
        _hubContext = hubContext;
    }

    // GET /Interview/Room?participantId=... (chỉ Sinh viên)
    [Authorize(Roles = Roles.Student)]
    public IActionResult Room(int participantId)
    {
        ViewBag.ParticipantId = participantId;
        return View();
    }

    // GET /Interview/Transcript?participantId=... (Sinh viên của lượt đó, Admin, Giảng viên phụ trách)
    public async Task<IActionResult> Transcript(int participantId)
    {
        var result = await _interviewService.GetTranscriptAsync(CurrentUserId, participantId);
        if (!result.Succeeded)
        {
            return MapFailure(result) ?? BadRequest(result.Error);
        }

        return View(result.Data);
    }

    // POST /Interview/Start: Bắt đầu hoặc tiếp tục phỏng vấn
    [HttpPost]
    [Authorize(Roles = Roles.Student)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start([FromForm] int participantId)
    {
        var result = await _interviewService.StartAsync(CurrentUserId, participantId);
        if (!result.Succeeded)
        {
            return StatusCode(result.ErrorType switch
            {
                ServiceErrorType.NotFound => StatusCodes.Status404NotFound,
                ServiceErrorType.Forbidden => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            }, new { error = result.Error });
        }

        var state = result.Data!;
        var name = User.Identity?.Name ?? "Thí sinh";
        await _hubContext.NotifyAsync($"Thí sinh {name} đang thi.", LiveTopics.Exam(state.SessionId));

        return Json(state);
    }

    // POST /Interview/Answer: nộp câu trả lời, nhận lượt tiếp theo
    [HttpPost]
    [Authorize(Roles = Roles.Student)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Answer([FromForm] int turnId, [FromForm] string? answer, [FromForm] bool timedOut)
    {
        var result = await _interviewService.SubmitAnswerAsync(CurrentUserId, turnId, answer, timedOut);
        if (!result.Succeeded)
        {
            return StatusCode(result.ErrorType switch
            {
                ServiceErrorType.NotFound => StatusCodes.Status404NotFound,
                ServiceErrorType.Forbidden => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            }, new { error = result.Error });
        }

        var state = result.Data!;
        var name = User.Identity?.Name ?? "Thí sinh";
        var message = state.Finished
            ? $"Thí sinh {name} đã thi xong."
            : $"Thí sinh {name} đang thi.";

        await _hubContext.NotifyAsync(message, LiveTopics.Exam(state.SessionId));

        return Json(state);
    }

    // POST /Interview/Speak: luồng audio/pcm 48 kHz hoặc 503
    [HttpPost]
    [Authorize(Roles = Roles.Student)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Speak([FromForm] int turnId)
    {
        var result = await _interviewService.SpeakAsync(CurrentUserId, turnId);
        if (!result.Succeeded)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = result.Error });
        }

        return File(result.Data!, "audio/pcm");
    }
}
