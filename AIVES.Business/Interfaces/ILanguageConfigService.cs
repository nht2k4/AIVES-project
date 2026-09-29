using AIVES.Business.Common;
using AIVES.Business.DTOs;

namespace AIVES.Business.Interfaces;

public interface ILanguageConfigService
{
    Task<ServiceResult<LanguageConfigDto>> GetForEditAsync(int currentUserId, int subjectId);
    Task<ServiceResult> UpdateAsync(int currentUserId, UpdateLanguageRequest request);
    Task<ServiceResult<SpeechConfig>> GetSpeechConfigAsync(int subjectId);
}
