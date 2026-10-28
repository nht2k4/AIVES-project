using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.Business.Services;

// TV3 - Ass3 - việc 3.1. Xem AIVES.Tests/Business/SubjectTests.cs (SubjectServiceTests).
public class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _subjects;
    private readonly IAccountRepository _accounts;

    public SubjectService(ISubjectRepository subjects, IAccountRepository accounts)
    {
        _subjects = subjects;
        _accounts = accounts;
    }

    public async Task<List<SubjectDto>> GetAllAsync() =>
        (await _subjects.GetAllWithAssignmentsAsync()).Select(s => s.ToDto()).ToList();

    public async Task<List<SubjectDto>> GetByLecturerAsync(int lecturerId) =>
        (await _subjects.GetByLecturerAsync(lecturerId)).Select(s => s.ToDto()).ToList();

    public Task<ServiceResult<SubjectDetailDto>> GetDetailAsync(int subjectId) => throw new NotImplementedException();
}