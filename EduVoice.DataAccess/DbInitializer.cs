using EduVoice.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduVoice.DataAccess;

public static class DbInitializer
{
    // hashPassword được truyền từ tầng Business vào, để DataAccess không cần biết thuật toán băm
    public static async Task InitializeAsync(AppDbContext context, Func<string, string> hashPassword)
    {
        if (!context.Database.GetMigrations().Any())
            throw new InvalidOperationException(
                "Chưa có migration. Chạy: dotnet ef migrations add InitialCreate " +
                "--project EduVoice.DataAccess --startup-project EduVoice.WebMVC");

        await context.Database.MigrateAsync();

        if (await context.Accounts.AnyAsync()) return;

        var admin = new Account
        {
            FullName = "Quản trị hệ thống",
            Email = "admin@fu.edu.vn",
            PasswordHash = hashPassword("Admin@123"),
            Role = UserRole.Admin
        };
        var lecturerAn = new Account
        {
            FullName = "Nguyễn Văn An",
            Email = "an.nv@fu.edu.vn",
            PasswordHash = hashPassword("Lecturer@123"),
            Role = UserRole.Lecturer
        };
        var lecturerBinh = new Account
        {
            FullName = "Trần Thị Bình",
            Email = "binh.tt@fu.edu.vn",
            PasswordHash = hashPassword("Lecturer@123"),
            Role = UserRole.Lecturer
        };

        var prn222 = new Subject { Code = "PRN222", Name = "Advanced Cross-Platform Application Programming With .NET" };
        var swp391 = new Subject { Code = "SWP391", Name = "Software Development Project" };
        var enw492 = new Subject
        {
            Code = "ENW492c",
            Name = "Writing Research Papers",
            SttLanguage = SpeechLanguage.English,
            TtsLanguage = SpeechLanguage.English
        };
        var ssg104 = new Subject { Code = "SSG104", Name = "Communication and In-Group Working Skills" };
        var prn211 = new Subject { Code = "PRN211", Name = "Basic Cross-Platform Application Programming With .NET", IsActive = false };

        context.Accounts.AddRange(admin, lecturerAn, lecturerBinh);
        context.Subjects.AddRange(prn222, swp391, enw492, ssg104, prn211);
        context.LecturerSubjects.Add(new LecturerSubject { Lecturer = lecturerAn, Subject = prn222 });
        context.LecturerSubjects.Add(new LecturerSubject { Lecturer = lecturerBinh, Subject = enw492 });

        await context.SaveChangesAsync();
    }
}
