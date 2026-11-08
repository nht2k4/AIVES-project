using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVES.Ass3.MVC.Controllers.Api;

// TV5 - Ass3 - việc 5.4: môn học, phân công, ngôn ngữ (B3.5). Kiểm bằng ass3.http mục 3, 4.
// [Authorize(Roles)] chỉ chặn sớm theo vai trò; quyền theo môn (giảng viên chỉ môn được phân công) do Service quyết định.
[Route("api/subjects")]
[Authorize(Roles = Roles.Admin + "," + Roles.Lecturer)]
public class SubjectsApiController : ApiControllerBase
{
    private readonly ISubjectService _subjects;
    private readonly IAssignmentService _assignments;
    private readonly ILanguageConfigService _languages;

    public SubjectsApiController(ISubjectService subjects, IAssignmentService assignments, ILanguageConfigService languages)
    {
        _subjects = subjects;
        _assignments = assignments;
        _languages = languages;
    }

    // Admin: mọi môn. Giảng viên: chỉ môn được phân công.
    [HttpGet]
    public async Task<ActionResult<List<SubjectDto>>> GetAll() =>
        Ok(User.IsInRole(Roles.Admin)
            ? await _subjects.GetAllAsync()
            : await _subjects.GetByLecturerAsync(CurrentUserId));

    [HttpGet("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<SubjectDetailDto>> GetDetail(int id) => Respond(await _subjects.GetDetailAsync(id));

    [HttpPost("{id:int}/lecturers")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult> Assign(int id, [FromBody] AssignLecturerBody body) =>
        Respond(await _assignments.AssignAsync(body.LecturerId, id));

    [HttpDelete("{id:int}/lecturers/{lecturerId:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult> Unassign(int id, int lecturerId) =>
        Respond(await _assignments.UnassignAsync(lecturerId, id));

    [HttpGet("{id:int}/language")]
    public async Task<ActionResult<LanguageConfigDto>> GetLanguage(int id) =>
        Respond(await _languages.GetForEditAsync(CurrentUserId, id));

    // Trường null = giữ nguyên (ttsVoice, sttTerms, aiTimeoutSeconds, useExternalAi)
    [HttpPut("{id:int}/language")]
    public async Task<ActionResult> UpdateLanguage(int id, [FromBody] UpdateLanguageBody body) =>
        Respond(await _languages.UpdateAsync(CurrentUserId, new UpdateLanguageRequest(
            id, body.SttLanguage, body.TtsLanguage, body.TtsVoice, body.SttTerms, body.AiTimeoutSeconds, body.UseExternalAi)));

    // Cấu hình giọng nói/AI của môn dùng cho phòng thi. GetSpeechConfigAsync không nhận người gọi,
    // nên kiểm quyền theo môn trước bằng GetForEditAsync (cùng luật với trang Ngôn ngữ).
    [HttpGet("{id:int}/speech")]
    public async Task<ActionResult<SpeechConfig>> GetSpeech(int id)
    {
        var access = await _languages.GetForEditAsync(CurrentUserId, id);
        if (!access.Succeeded) return Respond(access);
        return Respond(await _languages.GetSpeechConfigAsync(id));
    }
}

public record AssignLecturerBody(int LecturerId);

public record UpdateLanguageBody(
    AppLanguage SttLanguage, AppLanguage TtsLanguage, string? TtsVoice = null,
    string? SttTerms = null, int? AiTimeoutSeconds = null, bool? UseExternalAi = null);
