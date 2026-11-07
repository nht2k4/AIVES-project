using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.DataAccess.Repositories;

// TV2 - Ass3 - việc 2.1. Xem AIVES.Tests.Data/RepositoryTests.cs (QuestionRepositoryTests).
public class QuestionRepository : IQuestionRepository
{
    private readonly AppDbContext _context;

    public QuestionRepository(AppDbContext context) => _context = context;

    public Task<List<Question>> GetBySubjectAsync(int subjectId, bool onlyActive) => throw new NotImplementedException();

    public Task<Question?> GetByIdAsync(int id) => throw new NotImplementedException();

    public Task AddAsync(Question question) => throw new NotImplementedException();

    public Task UpdateAsync(Question question) => throw new NotImplementedException();
}
