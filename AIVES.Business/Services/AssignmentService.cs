using AIVES.Business.Common;
using AIVES.Business.Interfaces;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.Business.Services;

// TV3 - Ass3 - việc 3.1. Xem AIVES.Tests/Business/SubjectTests.cs (AssignmentServiceTests).
public class AssignmentService : IAssignmentService
{
    public AssignmentService(IAccountRepository accounts, ISubjectRepository subjects, ILecturerSubjectRepository assignments)
    {
    }

    public Task<ServiceResult> AssignAsync(int lecturerId, int subjectId) => throw new NotImplementedException();

    public Task<ServiceResult> UnassignAsync(int lecturerId, int subjectId) => throw new NotImplementedException();

    public Task<bool> CanAccessSubjectAsync(int accountId, int subjectId) => throw new NotImplementedException();
}
