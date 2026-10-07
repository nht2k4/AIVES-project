using AIVES.Business.Common;
using AIVES.Business.DTOs;

namespace AIVES.Business.Interfaces;

public interface IQuestionService
{
    Task<ServiceResult<List<QuestionDto>>> GetBySubjectAsync(int currentUserId, int subjectId);
    Task<ServiceResult> AddAsync(int currentUserId, AddQuestionRequest request);
    Task<ServiceResult> SetActiveAsync(int currentUserId, int questionId, bool isActive);
}
