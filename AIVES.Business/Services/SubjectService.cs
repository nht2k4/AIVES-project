using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.Business.Services;

// TV3 - Ass2 - việc 3.3. Xem AIVES.Tests/Business/SubjectTests.cs (SubjectServiceTests).
public class SubjectService : ISubjectService
{
    public SubjectService(ISubjectRepository subjects, IAccountRepository accounts)
    {
    }

    public Task<List<SubjectDto>> GetAllAsync() => throw new NotImplementedException();

    public Task<List<SubjectDto>> GetByLecturerAsync(int lecturerId) => throw new NotImplementedException();

    public Task<ServiceResult<SubjectDetailDto>> GetDetailAsync(int subjectId) => throw new NotImplementedException();
}
