using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AIVES.DataAccess.Repositories;

// TV2 - Ass2 - việc 2.2. Xem AIVES.Tests.Data/RepositoryTests.cs (ExamRepositoryTests) và AIVES_PhanCong_Ass2_Ass3.md.
public class ExamRepository : IExamRepository
{
    private readonly AppDbContext _context;

    public ExamRepository(AppDbContext context) => _context = context;

    public async Task<List<ExamSession>> GetListAsync(IReadOnlyCollection<int>? subjectIds)
    {
        if (subjectIds is { Count: 0 })
            return new List<ExamSession>();

        var query = _context.ExamSessions.AsNoTracking()
            .Include(e => e.Subject)
            .Include(e => e.ExamParticipants)
            .AsQueryable();

        if (subjectIds is not null)
            query = query.Where(e => subjectIds.Contains(e.SubjectId));

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .ToListAsync();
    }

    public Task<ExamSession?> GetDetailAsync(int id) =>
        _context.ExamSessions
            .Include(e => e.Subject)
            .Include(e => e.ExamParticipants)
                .ThenInclude(p => p.ParticipantQuestions)
                    .ThenInclude(pq => pq.Question)
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task AddAsync(ExamSession session)
    {
        _context.ExamSessions.Add(session);
        await _context.SaveChangesAsync();
    }

    public Task SaveAsync() => _context.SaveChangesAsync();
}
