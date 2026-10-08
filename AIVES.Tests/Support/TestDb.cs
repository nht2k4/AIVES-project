using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using AIVES.DataAccess.Entities;

namespace AIVES.Tests.Support;

// "Database trong bộ nhớ" dùng chung cho mọi test: các repository giả đọc và ghi vào các danh sách ở đây.
// Test KHÔNG cần SQL Server, nên TV3 có thể làm tầng Business ngay cả khi database chưa xong.
public class TestDb
{
    private int _id;
    public int NextId() => ++_id;

    public List<Account> Accounts { get; } = new();
    public List<Subject> Subjects { get; } = new();
    public List<LecturerSubject> LecturerSubjects { get; } = new();
    public List<Question> Questions { get; } = new();
    public List<ExamSession> Sessions { get; } = new();
    public List<InterviewTurn> Turns { get; } = new();
    public List<Voice> Voices { get; } = new();
    public List<InterviewTurn> PendingTurns { get; } = new(); // lượt hỏi đã AddTurn nhưng chưa SaveAsync

    // Bộ băm GIẢ: để test của người khác không phải chờ TV3 làm xong Pbkdf2PasswordHasher. Hasher thật có test riêng (PasswordHasherTests).
    public IPasswordHasher Hasher { get; } = new FakePasswordHasher();

    // ---- Repository giả
    public InMemoryAccountRepository AccountRepo => new(this);
    public InMemorySubjectRepository SubjectRepo => new(this);
    public InMemoryLecturerSubjectRepository LecturerSubjectRepo => new(this);
    public InMemoryQuestionRepository QuestionRepo => new(this);
    public InMemoryExamRepository ExamRepo => new(this);
    public InMemoryInterviewRepository InterviewRepo => new(this);
    public InMemoryVoiceRepository VoiceRepo => new(this);

    // ---- Hàm dựng dữ liệu mẫu cho test
    public Account AddAccount(string role, string? email = null, bool active = true, string? studentCode = null,
        string? name = null, string password = "Password@1")
    {
        var id = NextId();
        var account = new Account
        {
            Id = id,
            FullName = name ?? $"{role} {id}",
            Email = email ?? $"{role.ToLowerInvariant()}{id}@fu.edu.vn",
            PasswordHash = Hasher.Hash(password),
            Role = role,
            IsActive = active,
            StudentCode = studentCode,
            CreatedAt = DateTime.UtcNow
        };
        Accounts.Add(account);
        return account;
    }

    public Subject AddSubject(string code, bool active = true, string stt = "vi-VN", string tts = "vi-VN")
    {
        var subject = new Subject { Id = NextId(), Code = code, Name = $"Môn {code}", IsActive = active, SttLanguage = stt, TtsLanguage = tts,
            AiTimeoutSeconds = 8, UseExternalAi = true }; // giống giá trị mặc định trong database
        Subjects.Add(subject);
        return subject;
    }

    public void Assign(Account lecturer, Subject subject)
    {
        var link = new LecturerSubject
        {
            LecturerId = lecturer.Id, SubjectId = subject.Id, AssignedAt = DateTime.UtcNow,
            Lecturer = lecturer, Subject = subject
        };
        LecturerSubjects.Add(link);
        subject.LecturerSubjects.Add(link);
        lecturer.LecturerSubjects.Add(link);
    }

    public Question AddQuestion(Subject subject, string content = "Câu hỏi?", string? points = null, bool active = true)
    {
        var q = new Question { Id = NextId(), SubjectId = subject.Id, Content = content, ExpectedPoints = points, IsActive = active, Subject = subject };
        Questions.Add(q);
        return q;
    }

    public List<Question> AddQuestions(Subject subject, int count) =>
        Enumerable.Range(1, count).Select(i => AddQuestion(subject, $"Câu hỏi số {i} của {subject.Code}")).ToList();

    // Tạo sẵn một phiên thi (đã có thí sinh và bộ câu hỏi) để test phần phỏng vấn
    public ExamSession AddSession(Subject subject, Account creator, int mainQuestions = 2, int maxFollowUps = 4, int maxPerQuestion = 2,
        int answerSeconds = 60, string status = ExamStatuses.Open, params string[] studentCodes)
    {
        var questions = AddQuestions(subject, Math.Max(mainQuestions, 3));
        var session = new ExamSession
        {
            Id = NextId(), SubjectId = subject.Id, Subject = subject, Title = "Phiên thi mẫu",
            StartTime = DateTime.Now.AddDays(1), SlotMinutes = 10, MainQuestionCount = mainQuestions,
            MaxFollowUps = maxFollowUps, MaxFollowUpsPerQuestion = maxPerQuestion, AnswerSeconds = answerSeconds,
            Status = status, CreatedBy = creator.Id
        };

        foreach (var (code, index) in (studentCodes.Length == 0 ? new[] { "SE1" } : studentCodes).Select((c, i) => (c, i)))
        {
            var participant = new ExamParticipant
            {
                Id = NextId(), SessionId = session.Id, Session = session, StudentCode = code, FullName = $"Sinh viên {code}",
                SlotStart = session.StartTime.AddMinutes(index * 10), SlotEnd = session.StartTime.AddMinutes(index * 10 + 10),
                Status = ParticipantStatuses.Scheduled
            };
            for (var n = 0; n < mainQuestions; n++)
                participant.ParticipantQuestions.Add(new ParticipantQuestion
                {
                    ParticipantId = participant.Id, QuestionId = questions[n].Id, OrderNo = n + 1,
                    Question = questions[n], Participant = participant
                });
            session.ExamParticipants.Add(participant);
        }

        Sessions.Add(session);
        return session;
    }
}
