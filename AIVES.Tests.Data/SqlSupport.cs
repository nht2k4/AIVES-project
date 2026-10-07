using AIVES.DataAccess;
using AIVES.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AIVES.Tests.Data;

// Test tích hợp với SQL Server THẬT: kiểm tra cả script database của Tien lẫn repository của TV2.
//
// Cách chạy: tạo database bằng hai script trong thư mục Database/ (nên dùng một database riêng để test, ví dụ AIVESDb_Test),
// rồi đặt biến môi trường trước khi chạy test:
//   PowerShell:  $env:AIVES_TEST_CONNECTION = "Server=.;Database=AIVESDb_Test;Trusted_Connection=True;TrustServerCertificate=True"
//   dotnet test AIVES.Tests.Data
// Chưa đặt biến này thì mọi test ở đây tự bỏ qua (Skipped), không báo lỗi.
// Mỗi test dùng dữ liệu có mã ngẫu nhiên nên không cần xóa dữ liệu sau khi chạy và chạy lại được nhiều lần.
public sealed class SqlFactAttribute : FactAttribute
{
    public SqlFactAttribute()
    {
        if (Sql.ConnectionString is null)
            Skip = "Chưa đặt biến môi trường AIVES_TEST_CONNECTION (xem hướng dẫn ở đầu file SqlSupport.cs).";
    }
}

public static class Sql
{
    public static string? ConnectionString => Environment.GetEnvironmentVariable("AIVES_TEST_CONNECTION");

    // Mỗi lần gọi trả về một DbContext MỚI: giống một request mới, không dính dữ liệu đã cache của lần trước
    public static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString!).Options);

    public static string Unique() => Guid.NewGuid().ToString("N")[..10];

    public static async Task<Account> SeedAccountAsync(string role = "Lecturer", bool active = true, string? name = null)
    {
        var u = Unique();
        var account = new Account
        {
            FullName = name ?? $"Người dùng {u}", Email = $"{u}@test.vn", PasswordHash = "100000.salt.key",
            Role = role, IsActive = active
        };
        await using var ctx = NewContext();
        ctx.Accounts.Add(account);
        await ctx.SaveChangesAsync();
        return account;
    }

    public static async Task<Subject> SeedSubjectAsync(bool active = true)
    {
        var subject = new Subject { Code = "T" + Unique(), Name = "Môn kiểm thử", IsActive = active, SttLanguage = "vi-VN", TtsLanguage = "vi-VN" };
        await using var ctx = NewContext();
        ctx.Subjects.Add(subject);
        await ctx.SaveChangesAsync();
        return subject;
    }

    public static async Task AssignAsync(int lecturerId, int subjectId)
    {
        await using var ctx = NewContext();
        ctx.LecturerSubjects.Add(new LecturerSubject { LecturerId = lecturerId, SubjectId = subjectId });
        await ctx.SaveChangesAsync();
    }

    public static async Task<Question> SeedQuestionAsync(int subjectId, string content = "Câu hỏi kiểm thử", bool active = true)
    {
        var question = new Question { SubjectId = subjectId, Content = content, IsActive = active };
        await using var ctx = NewContext();
        ctx.Questions.Add(question);
        await ctx.SaveChangesAsync();
        return question;
    }

    // Một phiên thi có sẵn các thí sinh, mỗi thí sinh có sẵn bộ câu hỏi
    public static async Task<ExamSession> SeedSessionAsync(Subject subject, Account creator, IReadOnlyList<Question> questions, params string[] studentCodes)
    {
        var session = new ExamSession
        {
            SubjectId = subject.Id, Title = "Phiên thi kiểm thử", StartTime = new DateTime(2030, 1, 1, 8, 0, 0), SlotMinutes = 10,
            MainQuestionCount = questions.Count, MaxFollowUps = 4, MaxFollowUpsPerQuestion = 2, AnswerSeconds = 60,
            Status = "Open", CreatedBy = creator.Id
        };
        for (var i = 0; i < studentCodes.Length; i++)
        {
            var p = new ExamParticipant
            {
                StudentCode = studentCodes[i], FullName = $"SV {studentCodes[i]}", Status = "Scheduled",
                SlotStart = session.StartTime.AddMinutes(i * 10), SlotEnd = session.StartTime.AddMinutes(i * 10 + 10)
            };
            for (var n = 0; n < questions.Count; n++)
                p.ParticipantQuestions.Add(new ParticipantQuestion { QuestionId = questions[n].Id, OrderNo = n + 1 });
            session.ExamParticipants.Add(p);
        }

        await using var ctx = NewContext();
        ctx.ExamSessions.Add(session);
        await ctx.SaveChangesAsync();
        return session;
    }
}

public sealed class SqlTheoryAttribute : TheoryAttribute
{
    public SqlTheoryAttribute()
    {
        if (Sql.ConnectionString is null)
            Skip = "Chưa đặt biến môi trường AIVES_TEST_CONNECTION (xem hướng dẫn ở đầu file SqlSupport.cs).";
    }
}
