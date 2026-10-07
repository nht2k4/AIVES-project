using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.Business.Services;

// TV3 - Ass3 - việc 3.2. Xem AIVES.Tests/Business/ExamTests.cs (ExamServiceTests) và AIVES_PhanCong_Ass2_Ass3.md.
public class ExamService : IExamService
{
    public ExamService(IExamRepository exams, IQuestionRepository questions, IAccountRepository accounts,
        ISubjectRepository subjects, IAssignmentService assignmentService)
    {
    }

    public Task<List<ExamListItemDto>> GetListAsync(int currentUserId) => throw new NotImplementedException();

    public Task<List<SubjectDto>> GetSubjectsForExamAsync(int currentUserId) => throw new NotImplementedException();

    public Task<ServiceResult<int>> CreateAsync(int currentUserId, CreateExamRequest request) => throw new NotImplementedException();

    public Task<ServiceResult<ExamDetailDto>> GetDetailAsync(int currentUserId, int examId) => throw new NotImplementedException();

    public Task<ServiceResult> SetOpenAsync(int currentUserId, int examId, bool open) => throw new NotImplementedException();

    public Task<ServiceResult> ReallocateAsync(int currentUserId, int examId) => throw new NotImplementedException();
}
