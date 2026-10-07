using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.Business.Services;

// TV3 - Ass2 - việc 3.3. Xem AIVES.Tests/Business/SubjectTests.cs (LanguageConfigServiceTests).
public class LanguageConfigService : ILanguageConfigService
{
    public LanguageConfigService(ISubjectRepository subjects, IAccountRepository accounts, IAssignmentService assignmentService)
    {
    }

    public Task<ServiceResult<LanguageConfigDto>> GetForEditAsync(int currentUserId, int subjectId) => throw new NotImplementedException();

    public Task<ServiceResult> UpdateAsync(int currentUserId, UpdateLanguageRequest request) => throw new NotImplementedException();

    public Task<ServiceResult<SpeechConfig>> GetSpeechConfigAsync(int subjectId) => throw new NotImplementedException();
}
