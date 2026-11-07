using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.DataAccess.Repositories;

// TV2 - Ass3 - việc 2.1. Xem AIVES.Tests.Data/RepositoryTests.cs (ExamRepositoryTests) và AIVES_PhanCong_Ass2_Ass3.md.
public class ExamRepository : IExamRepository
{
    private readonly AppDbContext _context;

    public ExamRepository(AppDbContext context) => _context = context;

    public Task<List<ExamSession>> GetListAsync(IReadOnlyCollection<int>? subjectIds) => throw new NotImplementedException();

    public Task<ExamSession?> GetDetailAsync(int id) => throw new NotImplementedException();

    public Task<List<ExamParticipant>> GetParticipantsByStudentCodeAsync(string studentCode) => throw new NotImplementedException();

    public Task AddAsync(ExamSession session) => throw new NotImplementedException();

    public Task SaveAsync() => throw new NotImplementedException();
}
