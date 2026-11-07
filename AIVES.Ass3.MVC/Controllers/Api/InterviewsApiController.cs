using AIVES.Ass3.MVC.Infrastructure;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace AIVES.Ass3.MVC.Controllers.Api;

[Route("api/interviews")]
public class InterviewsApiController : ApiControllerBase
{
    private readonly IInterviewService _interviewService;
    private readonly IHubContext<LiveHub> _hubContext;

    public InterviewsApiController(IInterviewService interviewService, IHubContext<LiveHub> hubContext)
    {
        _interviewService = interviewService;
        _hubContext = hubContext;
    }

    // GET /api/interviews/my: chỉ Sinh viên
    [HttpGet("my")]
    public async Task<IActionResult> GetMyInterviews()
    {
        if (!User.IsInRole(Roles.Student))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Chỉ sinh viên mới có danh sách bài thi." });
        }

        var interviews = await _interviewService.GetMyInterviewsAsync(CurrentUserId);
        return Ok(interviews);
    }

    // POST /api/interviews/{participantId}/start: chỉ chính sinh viên làm bài
    [HttpPost("{participantId:int}/start")]
    public async Task<IActionResult> Start(int participantId)
    {
        var result = await _interviewService.StartAsync(CurrentUserId, participantId);
        if (!result.Succeeded)
        {
            return Respond(result);
        }

        var state = result.Data!;
        var name = User.Identity?.Name ?? "Thí sinh";
        await _hubContext.NotifyAsync($"Thí sinh {name} đang thi.", LiveTopics.Exam(state.SessionId));

        return Ok(state);
    }

    public record AnswerApiRequest(string? Answer, bool TimedOut);

    // POST /api/interviews/turns/{turnId}/answer: nộp câu trả lời, nhận lượt tiếp theo
    [HttpPost("turns/{turnId:int}/answer")]
    public async Task<IActionResult> Answer(int turnId, [FromBody] AnswerApiRequest request)
    {
        var result = await _interviewService.SubmitAnswerAsync(CurrentUserId, turnId, request?.Answer, request?.TimedOut ?? false);
        if (!result.Succeeded)
        {
            return Respond(result);
        }

        var state = result.Data!;
        var name = User.Identity?.Name ?? "Thí sinh";
        var message = state.Finished
            ? $"Thí sinh {name} đã thi xong."
            : $"Thí sinh {name} đang thi.";

        await _hubContext.NotifyAsync(message, LiveTopics.Exam(state.SessionId));

        return Ok(state);
    }

    // GET /api/interviews/turns/{turnId}/speech: luồng audio/pcm 48 kHz hoặc 503
    [HttpGet("turns/{turnId:int}/speech")]
    public async Task<IActionResult> Speech(int turnId)
    {
        var result = await _interviewService.SpeakAsync(CurrentUserId, turnId);
        if (!result.Succeeded)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = result.Error });
        }

        return File(result.Data!, "audio/pcm");
    }

    // GET /api/interviews/{participantId}/transcript: sinh viên của lượt đó, admin hoặc giảng viên phụ trách môn
    [HttpGet("{participantId:int}/transcript")]
    public async Task<IActionResult> Transcript(int participantId)
    {
        var result = await _interviewService.GetTranscriptAsync(CurrentUserId, participantId);
        return Respond(result);
    }
}
