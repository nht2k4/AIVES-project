using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AIVES.DataAccess.Repositories;

// TV2 - Ass3 - việc 2.2. Xem AIVES.Tests.Data/RepositoryTests.cs (InterviewRepositoryTests) và AIVES_PhanCong_Ass2_Ass3.md.
public class InterviewRepository : IInterviewRepository
{
    private readonly AppDbContext _context;

    public InterviewRepository(AppDbContext context) => _context = context;

    public Task<ExamParticipant?> GetParticipantAsync(int participantId) =>
        _context.ExamParticipants
            .Include(p => p.Session).ThenInclude(s => s.Subject)
            .Include(p => p.ParticipantQuestions).ThenInclude(pq => pq.Question)
            .FirstOrDefaultAsync(p => p.Id == participantId);

    public Task<InterviewTurn?> GetTurnAsync(int turnId) =>
        _context.InterviewTurns.Include(t => t.Question).FirstOrDefaultAsync(t => t.Id == turnId);

    public Task<List<InterviewTurn>> GetTurnsAsync(int participantId) =>
        _context.InterviewTurns.Where(t => t.ParticipantId == participantId).OrderBy(t => t.Id).ToListAsync();

    public Task<bool> HasTurnsAsync(int participantId) =>
        _context.InterviewTurns.AnyAsync(t => t.ParticipantId == participantId);

    public void AddTurn(InterviewTurn turn) => _context.InterviewTurns.Add(turn);

    public Task<TurnRecording?> GetRecordingAsync(int turnId) =>
        _context.TurnRecordings.AsNoTracking().FirstOrDefaultAsync(r => r.TurnId == turnId);

    public Task<List<int>> GetRecordedTurnIdsAsync(int participantId) =>
        _context.TurnRecordings.Where(r => r.Turn.ParticipantId == participantId).Select(r => r.TurnId).ToListAsync();

    public void AddRecording(TurnRecording recording) => _context.TurnRecordings.Add(recording);

    public Task SaveAsync() => _context.SaveChangesAsync();
}
