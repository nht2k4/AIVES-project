using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.DataAccess.Repositories;

// TV2 - Ass3 - việc 2.2. Xem AIVES.Tests.Data/RepositoryTests.cs (InterviewRepositoryTests) và AIVES_PhanCong_Ass2_Ass3.md.
public class InterviewRepository : IInterviewRepository
{
    private readonly AppDbContext _context;

    public InterviewRepository(AppDbContext context) => _context = context;

    public Task<ExamParticipant?> GetParticipantAsync(int participantId) => throw new NotImplementedException();

    public Task<InterviewTurn?> GetTurnAsync(int turnId) => throw new NotImplementedException();

    public Task<List<InterviewTurn>> GetTurnsAsync(int participantId) => throw new NotImplementedException();

    public Task<bool> HasTurnsAsync(int participantId) => throw new NotImplementedException();

    public void AddTurn(InterviewTurn turn) => throw new NotImplementedException();

    public Task SaveAsync() => throw new NotImplementedException();
}
