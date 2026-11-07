using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.Business.Services;

// TV5 - Ass3 - việc 5.1. Xem AIVES.Tests/Business/ExamTests.cs (QuestionServiceTests).
public class QuestionService : IQuestionService
{
    public QuestionService(IQuestionRepository questions, IAssignmentService assignmentService)
    {
    }

    public Task<ServiceResult<List<QuestionDto>>> GetBySubjectAsync(int currentUserId, int subjectId) => throw new NotImplementedException();

    public Task<ServiceResult> AddAsync(int currentUserId, AddQuestionRequest request) => throw new NotImplementedException();

    public Task<ServiceResult> SetActiveAsync(int currentUserId, int questionId, bool isActive) => throw new NotImplementedException();
}
