using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.Tests.Support;

// Cài đặt repository bằng danh sách trong bộ nhớ. Entity được dùng chung theo tham chiếu, giống cách EF Core "theo dõi thay đổi":
// Service sửa entity rồi gọi Update/Save thì dữ liệu trong TestDb đã thay đổi.

public class InMemoryAccountRepository : IAccountRepository
{
    private readonly TestDb _db;
    public InMemoryAccountRepository(TestDb db) => _db = db;

    public Task<Account?> GetByIdAsync(int id) => Task.FromResult(_db.Accounts.FirstOrDefault(a => a.Id == id));

    public Task<Account?> GetByEmailAsync(string email) =>
        Task.FromResult(_db.Accounts.FirstOrDefault(a => string.Equals(a.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> EmailExistsAsync(string email, int? excludeId = null) =>
        Task.FromResult(_db.Accounts.Any(a => string.Equals(a.Email, email, StringComparison.OrdinalIgnoreCase) && (excludeId == null || a.Id != excludeId)));

    public Task<List<Account>> SearchAsync(string? keyword, string? role)
    {
        var query = _db.Accounts.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(a => a.FullName.Contains(k, StringComparison.OrdinalIgnoreCase) || a.Email.Contains(k, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrEmpty(role)) query = query.Where(a => a.Role == role);
        return Task.FromResult(query.OrderBy(a => a.Role).ThenBy(a => a.FullName).ToList());
    }

    public Task<List<Account>> GetActiveByRoleAsync(string role) =>
        Task.FromResult(_db.Accounts.Where(a => a.Role == role && a.IsActive).OrderBy(a => a.FullName).ToList());

    public Task<int> CountActiveByRoleAsync(string role) =>
        Task.FromResult(_db.Accounts.Count(a => a.Role == role && a.IsActive));

    public Task AddAsync(Account account)
    {
        account.Id = _db.NextId();
        if (account.CreatedAt == default) account.CreatedAt = DateTime.UtcNow;
        _db.Accounts.Add(account);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Account account) => Task.CompletedTask;
}

public class InMemorySubjectRepository : ISubjectRepository
{
    private readonly TestDb _db;
    public InMemorySubjectRepository(TestDb db) => _db = db;

    public Task<Subject?> GetByIdAsync(int id) => Task.FromResult(_db.Subjects.FirstOrDefault(s => s.Id == id));

    public Task<Subject?> GetWithLecturersAsync(int id) => GetByIdAsync(id); // LecturerSubjects + Lecturer đã được TestDb.Assign gắn sẵn

    public Task<List<Subject>> GetAllWithAssignmentsAsync() => Task.FromResult(_db.Subjects.OrderBy(s => s.Code).ToList());

    public Task<List<Subject>> GetByLecturerAsync(int lecturerId) =>
        Task.FromResult(_db.Subjects.Where(s => s.LecturerSubjects.Any(ls => ls.LecturerId == lecturerId)).OrderBy(s => s.Code).ToList());

    public Task UpdateAsync(Subject subject) => Task.CompletedTask;
}

public class InMemoryLecturerSubjectRepository : ILecturerSubjectRepository
{
    private readonly TestDb _db;
    public InMemoryLecturerSubjectRepository(TestDb db) => _db = db;

    public Task<bool> ExistsAsync(int lecturerId, int subjectId) =>
        Task.FromResult(_db.LecturerSubjects.Any(x => x.LecturerId == lecturerId && x.SubjectId == subjectId));

    public Task<int> CountByLecturerAsync(int lecturerId) =>
        Task.FromResult(_db.LecturerSubjects.Count(x => x.LecturerId == lecturerId));

    public Task AddAsync(LecturerSubject entity)
    {
        var lecturer = _db.Accounts.First(a => a.Id == entity.LecturerId);
        var subject = _db.Subjects.First(s => s.Id == entity.SubjectId);
        entity.Lecturer = lecturer;
        entity.Subject = subject;
        if (entity.AssignedAt == default) entity.AssignedAt = DateTime.UtcNow;
        _db.LecturerSubjects.Add(entity);
        subject.LecturerSubjects.Add(entity);
        lecturer.LecturerSubjects.Add(entity);
        return Task.CompletedTask;
    }

    public Task<bool> RemoveAsync(int lecturerId, int subjectId)
    {
        var link = _db.LecturerSubjects.FirstOrDefault(x => x.LecturerId == lecturerId && x.SubjectId == subjectId);
        if (link is null) return Task.FromResult(false);

        _db.LecturerSubjects.Remove(link);
        link.Subject.LecturerSubjects.Remove(link);
        link.Lecturer.LecturerSubjects.Remove(link);
        return Task.FromResult(true);
    }
}

public class InMemoryQuestionRepository : IQuestionRepository
{
    private readonly TestDb _db;
    public InMemoryQuestionRepository(TestDb db) => _db = db;

    public Task<List<Question>> GetBySubjectAsync(int subjectId, bool onlyActive) =>
        Task.FromResult(_db.Questions.Where(q => q.SubjectId == subjectId && (!onlyActive || q.IsActive)).OrderBy(q => q.Id).ToList());

    public Task<Question?> GetByIdAsync(int id) => Task.FromResult(_db.Questions.FirstOrDefault(q => q.Id == id));

    public Task AddAsync(Question question)
    {
        question.Id = _db.NextId();
        question.Subject = _db.Subjects.FirstOrDefault(s => s.Id == question.SubjectId)!;
        _db.Questions.Add(question);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Question question) => Task.CompletedTask;
}

public class InMemoryExamRepository : IExamRepository
{
    private readonly TestDb _db;
    public InMemoryExamRepository(TestDb db) => _db = db;

    public Task<List<ExamSession>> GetListAsync(IReadOnlyCollection<int>? subjectIds)
    {
        foreach (var s in _db.Sessions) s.Subject = _db.Subjects.First(x => x.Id == s.SubjectId);
        return Task.FromResult(_db.Sessions
            .Where(s => subjectIds == null || subjectIds.Contains(s.SubjectId))
            .OrderByDescending(s => s.StartTime).ToList());
    }

    public Task<ExamSession?> GetDetailAsync(int id)
    {
        var session = _db.Sessions.FirstOrDefault(s => s.Id == id);
        if (session is null) return Task.FromResult<ExamSession?>(null);

        Hydrate(session);
        return Task.FromResult<ExamSession?>(session);
    }

    public Task AddAsync(ExamSession session)
    {
        session.Id = _db.NextId();
        foreach (var p in session.ExamParticipants)
        {
            p.Id = _db.NextId();
            p.SessionId = session.Id;
        }
        foreach (var pq in session.ExamParticipants.SelectMany(p => p.ParticipantQuestions))
            pq.ParticipantId = pq.Participant?.Id ?? pq.ParticipantId;

        _db.Sessions.Add(session);
        return Task.CompletedTask;
    }

    public Task SaveAsync() => Task.CompletedTask;

    private void Hydrate(ExamSession session)
    {
        session.Subject = _db.Subjects.First(s => s.Id == session.SubjectId);
        foreach (var p in session.ExamParticipants)
        {
            p.Session = session;
            foreach (var pq in p.ParticipantQuestions)
            {
                pq.Participant = p;
                pq.ParticipantId = p.Id;
                pq.Question = _db.Questions.First(q => q.Id == pq.QuestionId);
            }
        }
    }
}
