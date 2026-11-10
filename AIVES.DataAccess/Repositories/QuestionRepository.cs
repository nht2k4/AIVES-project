using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AIVES.DataAccess.Repositories;

// TV2 - Ass2 - việc 2.2. Xem AIVES.Tests.Data/RepositoryTests.cs (QuestionRepositoryTests).
public class QuestionRepository : IQuestionRepository
{
    private readonly AppDbContext _context;

    public QuestionRepository(AppDbContext context) => _context = context;

    public async Task<List<Question>> GetBySubjectAsync(int subjectId, bool onlyActive)
    {
        var query = _context.Questions.AsNoTracking().Where(q => q.SubjectId == subjectId);
        if (onlyActive)
            query = query.Where(q => q.IsActive);

        return await query.OrderBy(q => q.Id).ToListAsync();
    }

    public Task<Question?> GetByIdAsync(int id) =>
        _context.Questions.FirstOrDefaultAsync(q => q.Id == id);

    public async Task AddAsync(Question question)
    {
        _context.Questions.Add(question);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Question question)
    {
        var entry = _context.Entry(question);
        if (entry.State == EntityState.Detached)
            _context.Questions.Update(question);

        await _context.SaveChangesAsync();
    }
}
