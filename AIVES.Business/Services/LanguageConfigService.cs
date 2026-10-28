using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.Business.Services;

// TV3 - Ass3 - việc 3.1. Xem AIVES.Tests/Business/SubjectTests.cs (LanguageConfigServiceTests).
public class LanguageConfigService : ILanguageConfigService
{
    private readonly ISubjectRepository _subjects;
    private readonly IAccountRepository _accounts;
    private readonly IAssignmentService _assignmentService;

    public LanguageConfigService(ISubjectRepository subjects, IAccountRepository accounts, IAssignmentService assignmentService)
    {
        _subjects = subjects;
        _accounts = accounts;
        _assignmentService = assignmentService;
    }

    public async Task<ServiceResult<LanguageConfigDto>> GetForEditAsync(int currentUserId, int subjectId)
    {
        var subject = await _subjects.GetByIdAsync(subjectId);
        if (subject is null)
            return ServiceResult<LanguageConfigDto>.NotFound("Không tìm thấy môn học.");

        if (!await _assignmentService.CanAccessSubjectAsync(currentUserId, subjectId))
            return ServiceResult<LanguageConfigDto>.Forbidden("Bạn chưa được phân công môn học này.");

        return ServiceResult<LanguageConfigDto>.Ok(subject.ToLanguageDto());
    }

    // Mỗi dòng không trống phải có dạng "vế sai => vế đúng"
    private static bool IsValidTerms(string terms)
    {
        foreach (var raw in terms.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;

            var idx = line.IndexOf("=>", StringComparison.Ordinal);
            if (idx < 0) return false;
            if (line[..idx].Trim().Length == 0 || line[(idx + 2)..].Trim().Length == 0) return false;
        }
        return true;
    }

    public async Task<ServiceResult> UpdateAsync(int currentUserId, UpdateLanguageRequest request)
    {
        if (!Enum.IsDefined(request.SttLanguage) || !Enum.IsDefined(request.TtsLanguage))
            return ServiceResult.Fail("Ngôn ngữ không hợp lệ. Chỉ hỗ trợ Tiếng Việt và English.");

        // null = giữ nguyên; rỗng = về mặc định; có tên = đổi giọng
        var voice = request.TtsVoice?.Trim();
        if (voice is not null && voice.Length > 64)
            return ServiceResult.Fail("Tên giọng đọc tối đa 64 ký tự.");

        var terms = request.SttTerms?.Trim();
        if (!string.IsNullOrEmpty(terms) && !IsValidTerms(terms))
            return ServiceResult.Fail("Từ điển thuật ngữ: mỗi dòng phải có dạng \"cách nghe sai => cách viết đúng\".");

        if (request.AiTimeoutSeconds is < 2 or > 30)
            return ServiceResult.Fail("Thời gian chờ AI phải từ 2 đến 30 giây.");

        var subject = await _subjects.GetByIdAsync(request.SubjectId);
        if (subject is null)
            return ServiceResult.NotFound("Không tìm thấy môn học.");

        // Kiểm tra quyền ở tầng Business, không chỉ dựa vào [Authorize] ở Controller
        if (!await _assignmentService.CanAccessSubjectAsync(currentUserId, request.SubjectId))
            return ServiceResult.Forbidden("Bạn chưa được phân công môn học này.");

        if (!subject.IsActive)
            return ServiceResult.Fail($"Môn {subject.Code} đã ngừng hoạt động, không thể đổi cấu hình.");

        var actor = await _accounts.GetByIdAsync(currentUserId);

        subject.SttLanguage = request.SttLanguage.ToLocaleCode();
        subject.TtsLanguage = request.TtsLanguage.ToLocaleCode();
        subject.LanguageUpdatedAt = DateTime.UtcNow;
        subject.LanguageUpdatedBy = actor?.Email;

        if (voice is not null)
            subject.TtsVoice = voice.Length == 0 ? null : voice;
        if (terms is not null)
            subject.SttTerms = terms.Length == 0 ? null : terms;
        if (request.AiTimeoutSeconds.HasValue)
            subject.AiTimeoutSeconds = request.AiTimeoutSeconds.Value;
        if (request.UseExternalAi.HasValue)
            subject.UseExternalAi = request.UseExternalAi.Value;

        await _subjects.UpdateAsync(subject);
        return ServiceResult.Ok();
    }
    public async Task<ServiceResult<SpeechConfig>> GetSpeechConfigAsync(int subjectId)
    {
        var subject = await _subjects.GetByIdAsync(subjectId);
        if (subject is null)
            return ServiceResult<SpeechConfig>.NotFound("Không tìm thấy môn học.");

        return ServiceResult<SpeechConfig>.Ok(new SpeechConfig(
            subject.Code, subject.SttLanguage, subject.TtsLanguage,
            subject.TtsVoice, subject.SttTerms, subject.AiTimeoutSeconds, subject.UseExternalAi));
    }
}