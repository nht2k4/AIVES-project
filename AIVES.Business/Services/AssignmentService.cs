using AIVES.Business.Common;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.Business.Services;

// TV3 - Ass3 - việc 3.1. Xem AIVES.Tests/Business/SubjectTests.cs (AssignmentServiceTests).
public class AssignmentService : IAssignmentService
{
    private readonly IAccountRepository _accounts;
    private readonly ISubjectRepository _subjects;
    private readonly ILecturerSubjectRepository _assignments;

    public AssignmentService(IAccountRepository accounts, ISubjectRepository subjects, ILecturerSubjectRepository assignments)
    {
        _accounts = accounts;
        _subjects = subjects;
        _assignments = assignments;
    }

    public Task<ServiceResult> AssignAsync(int lecturerId, int subjectId) => throw new NotImplementedException();

    public Task<ServiceResult> UnassignAsync(int lecturerId, int subjectId) => throw new NotImplementedException();

    public Task<bool> CanAccessSubjectAsync(int accountId, int subjectId) => throw new NotImplementedException();
}