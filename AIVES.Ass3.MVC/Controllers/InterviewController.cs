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

    // GET /Interview/DownloadTranscript?participantId=... (TV5 mini-task): cùng quyền với Transcript, trả file .txt
    public async Task<IActionResult> DownloadTranscript(int participantId)
    {
        var result = await _interviewService.GetTranscriptAsync(CurrentUserId, participantId);
        if (!result.Succeeded)
        {
            return MapFailure(result) ?? BadRequest(result.Error);
        }

        var transcript = result.Data!;
        var bytes = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
            .GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(TranscriptText.Build(transcript)))
            .ToArray();
        return File(bytes, "text/plain; charset=utf-8", TranscriptText.FileName(transcript));
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

    // POST /Interview/UploadRecording: phòng thi gửi ghi âm câu trả lời của lượt vừa nộp (chỉ Sinh viên dự thi)
    [HttpPost]
    [Authorize(Roles = Roles.Student)]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> UploadRecording([FromForm] int turnId, IFormFile? audio)
    {
        if (audio is null) return BadRequest(new { error = "Thiếu tệp ghi âm." });

        using var buffer = new MemoryStream();
        await audio.CopyToAsync(buffer);
        var result = await _interviewService.SaveRecordingAsync(CurrentUserId, turnId, buffer.ToArray(), audio.ContentType);
        if (result.Succeeded) return NoContent();

        return StatusCode(result.ErrorType switch
        {
            ServiceErrorType.NotFound => StatusCodes.Status404NotFound,
            ServiceErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        }, new { error = result.Error });
    }

    // GET /Interview/Recording?turnId=...: giảng viên phụ trách môn và Admin nghe lại ghi âm trong trang Biên bản
    public async Task<IActionResult> Recording(int turnId)
    {
        var result = await _interviewService.GetRecordingAsync(CurrentUserId, turnId);
        if (!result.Succeeded)
        {
            return MapFailure(result) ?? BadRequest(result.Error);
        }

        Response.Headers.CacheControl = "no-store"; // dữ liệu riêng tư: trình duyệt và proxy không lưu lại
        return File(result.Data!.Audio, result.Data.ContentType, enableRangeProcessing: true);
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
