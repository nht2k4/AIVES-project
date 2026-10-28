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

    public Task<ServiceResult> UpdateAsync(int currentUserId, UpdateLanguageRequest request) => throw new NotImplementedException();

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