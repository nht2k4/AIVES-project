using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.Business.Services;

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

    public async Task<ServiceResult> UpdateAsync(int currentUserId, UpdateLanguageRequest request)
    {
        if (!Enum.IsDefined(request.SttLanguage) || !Enum.IsDefined(request.TtsLanguage))
            return ServiceResult.Fail("Ngôn ngữ không hợp lệ. Chỉ hỗ trợ Tiếng Việt và English.");

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

        await _subjects.UpdateAsync(subject);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<SpeechConfig>> GetSpeechConfigAsync(int subjectId)
    {
        var subject = await _subjects.GetByIdAsync(subjectId);
        if (subject is null)
            return ServiceResult<SpeechConfig>.NotFound("Không tìm thấy môn học.");

        return ServiceResult<SpeechConfig>.Ok(new SpeechConfig(subject.Code, subject.SttLanguage, subject.TtsLanguage));
    }
}
