using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.Business.Services;

// TV3 - Ass3 - việc 3.2. Xem AIVES.Tests/Business/ExamTests.cs (ExamServiceTests) và AIVES_PhanCong_Ass2_Ass3.md.
public class ExamService : IExamService
{
    private readonly IExamRepository _exams;
    private readonly IQuestionRepository _questions;
    private readonly IAccountRepository _accounts;
    private readonly ISubjectRepository _subjects;
    private readonly IAssignmentService _assignmentService;

    public ExamService(IExamRepository exams, IQuestionRepository questions, IAccountRepository accounts,
        ISubjectRepository subjects, IAssignmentService assignmentService)
    {
        _exams = exams;
        _questions = questions;
        _accounts = accounts;
        _subjects = subjects;
        _assignmentService = assignmentService;
    }

    // null = mọi môn; danh sách rỗng = không môn nào
    private async Task<List<int>?> AllowedSubjectIdsAsync(int currentUserId)
    {
        var user = await _accounts.GetByIdAsync(currentUserId);
        if (user is null || !user.IsActive) return new List<int>();

        if (user.Role == Roles.Admin) return null;
        if (user.Role == Roles.Lecturer)
            return (await _subjects.GetByLecturerAsync(currentUserId)).Select(s => s.Id).ToList();

        return new List<int>();
    }

    public async Task<List<ExamListItemDto>> GetListAsync(int currentUserId)
    {
        var ids = await AllowedSubjectIdsAsync(currentUserId);
        var exams = await _exams.GetListAsync(ids);

        return exams
            .Select(e => new ExamListItemDto(
                e.Id, e.Title, e.Subject.Code, e.Subject.Name,
                e.StartTime, e.ExamParticipants.Count, e.Status))
            .ToList();
    }

    public async Task<List<SubjectDto>> GetSubjectsForExamAsync(int currentUserId)
    {
        var user = await _accounts.GetByIdAsync(currentUserId);
        if (user is null || !user.IsActive) return new List<SubjectDto>();

        IEnumerable<Subject> subjects;
        if (user.Role == Roles.Admin)
            subjects = await _subjects.GetAllWithAssignmentsAsync();
        else if (user.Role == Roles.Lecturer)
            subjects = await _subjects.GetByLecturerAsync(currentUserId);
        else
            return new List<SubjectDto>();

        return subjects.Where(s => s.IsActive).OrderBy(s => s.Code).Select(s => s.ToDto()).ToList();
    }

    public Task<ServiceResult<int>> CreateAsync(int currentUserId, CreateExamRequest request) => throw new NotImplementedException();

    public Task<ServiceResult<ExamDetailDto>> GetDetailAsync(int currentUserId, int examId) => throw new NotImplementedException();

    public Task<ServiceResult> SetOpenAsync(int currentUserId, int examId, bool open) => throw new NotImplementedException();

    public Task<ServiceResult> ReallocateAsync(int currentUserId, int examId) => throw new NotImplementedException();
}