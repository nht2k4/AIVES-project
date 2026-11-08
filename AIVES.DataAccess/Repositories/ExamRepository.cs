using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AIVES.DataAccess.Repositories;

// TV2 - Ass3 - việc 2.1.
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

    public Task<List<ExamParticipant>> GetParticipantsByStudentCodeAsync(string studentCode)
    {
        var code = studentCode.ToUpperInvariant();
        return _context.ExamParticipants.AsNoTracking()
            .Include(p => p.Session).ThenInclude(s => s.Subject)
            .Where(p => p.StudentCode.ToUpper() == code)
            .OrderBy(p => p.SlotStart).ThenBy(p => p.Id)
            .ToListAsync();
    }

    public async Task AddAsync(ExamSession session)
    {
        _context.ExamSessions.Add(session);
        await _context.SaveChangesAsync();
    }

    public Task SaveAsync() => _context.SaveChangesAsync();
}
