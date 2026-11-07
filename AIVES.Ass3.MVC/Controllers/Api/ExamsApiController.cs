using AIVES.Ass3.MVC.Infrastructure;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace AIVES.Ass3.MVC.Controllers.Api;

// TV5 - Ass3 - việc 5.4: phiên thi và ngân hàng câu hỏi (B3.5). Kiểm bằng ass3.http mục 5, 6.
// Sau mỗi thay đổi thành công gửi SignalR như ExamsController, để trang web đang mở tự cập nhật.
[Route("api/exams")]
[Authorize(Roles = Roles.Admin + "," + Roles.Lecturer)]
public class ExamsApiController : ApiControllerBase
{
    private readonly IExamService _exams;
    private readonly IQuestionService _questions;
    private readonly IHubContext<LiveHub> _live;

    public ExamsApiController(IExamService exams, IQuestionService questions, IHubContext<LiveHub> live)
    {
        _exams = exams;
        _questions = questions;
        _live = live;
    }

    [HttpGet]
    public async Task<ActionResult<List<ExamListItemDto>>> GetList() => Ok(await _exams.GetListAsync(CurrentUserId));

    // Các môn đang mở mà người gọi được tạo phiên thi
    [HttpGet("subjects")]
    public async Task<ActionResult<List<SubjectDto>>> GetSubjects() => Ok(await _exams.GetSubjectsForExamAsync(CurrentUserId));

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateExamRequest request)
    {
        var result = await _exams.CreateAsync(CurrentUserId, request);
        if (!result.Succeeded) return Respond(result);

        await _live.NotifyAsync("Có phiên thi mới.", LiveTopics.Exams);
        return StatusCode(StatusCodes.Status201Created, new { id = result.Data });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExamDetailDto>> GetDetail(int id) => Respond(await _exams.GetDetailAsync(CurrentUserId, id));

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult> SetStatus(int id, [FromBody] SetExamStatusBody body)
    {
        var result = await _exams.SetOpenAsync(CurrentUserId, id, body.Open);
        if (result.Succeeded)
            await _live.NotifyAsync(body.Open ? "Phiên thi đã mở." : "Phiên thi đã đóng.", LiveTopics.Exams, LiveTopics.Exam(id));
        return Respond(result);
    }

    [HttpPost("{id:int}/reallocate")]
    public async Task<ActionResult> Reallocate(int id)
    {
        var result = await _exams.ReallocateAsync(CurrentUserId, id);
        if (result.Succeeded)
            await _live.NotifyAsync("Đã chọn lại bộ câu hỏi.", LiveTopics.Exam(id));
        return Respond(result);
    }

    // ----- Ngân hàng câu hỏi -----

    [HttpGet("/api/subjects/{subjectId:int}/questions")]
    public async Task<ActionResult<List<QuestionDto>>> GetQuestions(int subjectId) =>
        Respond(await _questions.GetBySubjectAsync(CurrentUserId, subjectId));

    [HttpPost("/api/subjects/{subjectId:int}/questions")]
    public async Task<ActionResult> AddQuestion(int subjectId, [FromBody] AddQuestionBody body)
    {
        var result = await _questions.AddAsync(CurrentUserId, new AddQuestionRequest(subjectId, body.Content ?? string.Empty, body.ExpectedPoints));
        return result.Succeeded ? StatusCode(StatusCodes.Status201Created) : Respond(result);
    }

    [HttpPatch("/api/questions/{id:int}/active")]
    public async Task<ActionResult> SetQuestionActive(int id, [FromBody] SetQuestionActiveBody body) =>
        Respond(await _questions.SetActiveAsync(CurrentUserId, id, body.IsActive));
}

public record SetExamStatusBody(bool Open);

// Content để string? để nội dung thiếu hoặc rỗng đi qua Service (trả {"error": "Nhập nội dung câu hỏi."}) thay vì lỗi tự động của [ApiController]
public record AddQuestionBody(string? Content, string? ExpectedPoints = null);

public record SetQuestionActiveBody(bool IsActive);
