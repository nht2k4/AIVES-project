using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AIVES.Tests.Data;

// ====== TV2 và Tien: repository chạy đúng trên database thật ======

public class AccountRepositoryTests
{
    private static AccountRepository Repo(out Microsoft.EntityFrameworkCore.DbContext ctx)
    {
        var c = Sql.NewContext();
        ctx = c;
        return new AccountRepository(c);
    }

    [SqlFact]
    public async Task Add_ThenGetById_AndGetByEmail_WorksCaseInsensitively()
    {
        var u = Sql.Unique();
        var account = new Account { FullName = "Nguyễn An", Email = $"{u}@test.vn", PasswordHash = "x", Role = "Lecturer", IsActive = true };
        var repo = Repo(out var ctx1);
        await repo.AddAsync(account);
        Assert.True(account.Id > 0);
        await ctx1.DisposeAsync();

        var repo2 = Repo(out var ctx2);
        Assert.Equal("Nguyễn An", (await repo2.GetByIdAsync(account.Id))!.FullName);
        Assert.Equal(account.Id, (await repo2.GetByEmailAsync($"{u.ToUpperInvariant()}@TEST.VN"))!.Id);
        Assert.NotEqual(default, (await repo2.GetByIdAsync(account.Id))!.CreatedAt); // DEFAULT sysutcdatetime() của database
        await ctx2.DisposeAsync();
    }

    [SqlFact]
    public async Task Add_DuplicateEmail_IsRejectedByTheDatabase()
    {
        var existing = await Sql.SeedAccountAsync();
        var repo = Repo(out _);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            repo.AddAsync(new Account { FullName = "Trùng", Email = existing.Email, PasswordHash = "x", Role = "Lecturer", IsActive = true }));
    }

    [SqlFact]
    public async Task Add_InvalidRole_IsRejectedByTheDatabaseCheckConstraint()
    {
        var repo = Repo(out _);
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            repo.AddAsync(new Account { FullName = "X", Email = $"{Sql.Unique()}@test.vn", PasswordHash = "x", Role = "Hacker", IsActive = true }));
    }

    [SqlFact]
    public async Task Add_DuplicateStudentCode_IsRejected_ButManyAccountsWithoutACodeAreFine()
    {
        var code = "SV" + Sql.Unique();
        await Sql.SeedAccountAsync("Student", studentCode: code);
        await Sql.SeedAccountAsync("Lecturer");   // StudentCode NULL không bị tính là trùng
        await Sql.SeedAccountAsync("Lecturer");

        await Assert.ThrowsAsync<DbUpdateException>(() => Sql.SeedAccountAsync("Student", studentCode: code));
    }

    [SqlFact]
    public async Task EmailExists_And_StudentCodeExists_HonourExcludeId()
    {
        var code = "SV" + Sql.Unique();
        var a = await Sql.SeedAccountAsync("Student", studentCode: code);
        var repo = Repo(out _);

        Assert.True(await repo.EmailExistsAsync(a.Email));
        Assert.False(await repo.EmailExistsAsync(a.Email, excludeId: a.Id));
        Assert.True(await repo.StudentCodeExistsAsync(code));
        Assert.False(await repo.StudentCodeExistsAsync(code, excludeId: a.Id));
        Assert.False(await repo.EmailExistsAsync($"{Sql.Unique()}@khong-co.vn"));
    }

    [SqlFact]
    public async Task Search_FiltersByKeywordAndRole_AndOrdersByRoleThenName()
    {
        var u = Sql.Unique();
        await Sql.SeedAccountAsync("Lecturer", name: $"Bình {u}");
        await Sql.SeedAccountAsync("Lecturer", name: $"An {u}");
        await Sql.SeedAccountAsync("Admin", name: $"Quản trị {u}");
        var repo = Repo(out _);

        var all = await repo.SearchAsync(u, null);
        Assert.Equal(3, all.Count);
        Assert.Equal(new[] { "Admin", "Lecturer", "Lecturer" }, all.Select(a => a.Role));
        Assert.Equal($"An {u}", all[1].FullName);

        Assert.Equal(2, (await repo.SearchAsync(u, "Lecturer")).Count);
        Assert.Single(await repo.SearchAsync($"Bình {u}", null));
    }

    [SqlFact]
    public async Task GetActiveByRole_And_CountActiveByRole_SkipLockedAccounts()
    {
        var before = await new AccountRepository(Sql.NewContext()).CountActiveByRoleAsync("Student");
        await Sql.SeedAccountAsync("Student", studentCode: "SV" + Sql.Unique());
        await Sql.SeedAccountAsync("Student", active: false, studentCode: "SV" + Sql.Unique());
        var repo = Repo(out _);

        Assert.Equal(before + 1, await repo.CountActiveByRoleAsync("Student"));
        Assert.All(await repo.GetActiveByRoleAsync("Student"), a => Assert.True(a.IsActive));
    }

    [SqlFact]
    public async Task Update_IsPersisted()
    {
        var seeded = await Sql.SeedAccountAsync();
        var repo = Repo(out var ctx);
        var tracked = (await repo.GetByIdAsync(seeded.Id))!;
        tracked.FullName = "Tên mới";
        tracked.IsActive = false;
        await repo.UpdateAsync(tracked);
        await ctx.DisposeAsync();

        var reloaded = (await Repo(out _).GetByIdAsync(seeded.Id))!;
        Assert.Equal("Tên mới", reloaded.FullName);
        Assert.False(reloaded.IsActive);
    }
}

public class SubjectRepositoryTests
{
    [SqlFact]
    public async Task GetWithLecturers_LoadsAssignedLecturers()
    {
        var subject = await Sql.SeedSubjectAsync();
        var lecturer = await Sql.SeedAccountAsync();
        await Sql.AssignAsync(lecturer.Id, subject.Id);

        var loaded = await new SubjectRepository(Sql.NewContext()).GetWithLecturersAsync(subject.Id);

        var link = Assert.Single(loaded!.LecturerSubjects);
        Assert.Equal(lecturer.Email, link.Lecturer.Email); // Include + ThenInclude phải nạp luôn giảng viên
    }

    [SqlFact]
    public async Task GetAllWithAssignments_IncludesLinks_OrderedByCode()
    {
        var a = await Sql.SeedSubjectAsync();
        await Sql.AssignAsync((await Sql.SeedAccountAsync()).Id, a.Id);

        var all = await new SubjectRepository(Sql.NewContext()).GetAllWithAssignmentsAsync();

        Assert.Equal(all.OrderBy(s => s.Code, StringComparer.OrdinalIgnoreCase).Select(s => s.Id), all.Select(s => s.Id));
        Assert.Single(all.Single(s => s.Id == a.Id).LecturerSubjects);
    }

    [SqlFact]
    public async Task GetByLecturer_ReturnsOnlyThatLecturersSubjects()
    {
        var mine = await Sql.SeedSubjectAsync();
        var other = await Sql.SeedSubjectAsync();
        var lecturer = await Sql.SeedAccountAsync();
        await Sql.AssignAsync(lecturer.Id, mine.Id);
        await Sql.AssignAsync((await Sql.SeedAccountAsync()).Id, other.Id);

        var result = await new SubjectRepository(Sql.NewContext()).GetByLecturerAsync(lecturer.Id);

        Assert.Equal(mine.Id, Assert.Single(result).Id);
    }

    [SqlFact]
    public async Task Update_IsPersisted_IncludingTheNullableColumns()
    {
        var seeded = await Sql.SeedSubjectAsync();
        var ctx = Sql.NewContext();
        var repo = new SubjectRepository(ctx);
        var tracked = (await repo.GetByIdAsync(seeded.Id))!;
        tracked.SttLanguage = "en-US";
        tracked.TtsVoice = "Mai Anh";
        tracked.LanguageUpdatedAt = DateTime.UtcNow;
        await repo.UpdateAsync(tracked);

        var reloaded = (await new SubjectRepository(Sql.NewContext()).GetByIdAsync(seeded.Id))!;
        Assert.Equal(("en-US", "Mai Anh"), (reloaded.SttLanguage, reloaded.TtsVoice));
    }

    [SqlFact]
    public async Task LanguageColumns_RejectValuesOutsideViAndEn()
    {
        var seeded = await Sql.SeedSubjectAsync();
        var ctx = Sql.NewContext();
        var repo = new SubjectRepository(ctx);
        var tracked = (await repo.GetByIdAsync(seeded.Id))!;
        tracked.SttLanguage = "fr-FR";

        await Assert.ThrowsAsync<DbUpdateException>(() => repo.UpdateAsync(tracked));
    }
}

public class LecturerSubjectRepositoryTests
{
    [SqlFact]
    public async Task Add_Exists_Count_Remove()
    {
        var lecturer = await Sql.SeedAccountAsync();
        var subject = await Sql.SeedSubjectAsync();
        var repo = new LecturerSubjectRepository(Sql.NewContext());

        Assert.False(await repo.ExistsAsync(lecturer.Id, subject.Id));
        await repo.AddAsync(new LecturerSubject { LecturerId = lecturer.Id, SubjectId = subject.Id });

        var repo2 = new LecturerSubjectRepository(Sql.NewContext());
        Assert.True(await repo2.ExistsAsync(lecturer.Id, subject.Id));
        Assert.Equal(1, await repo2.CountByLecturerAsync(lecturer.Id));
        Assert.True(await repo2.RemoveAsync(lecturer.Id, subject.Id));
        Assert.False(await repo2.RemoveAsync(lecturer.Id, subject.Id)); // lần hai không còn gì để xóa
        Assert.False(await new LecturerSubjectRepository(Sql.NewContext()).ExistsAsync(lecturer.Id, subject.Id));
    }

    [SqlFact]
    public async Task Add_SamePairTwice_IsRejectedByThePrimaryKey()
    {
        var lecturer = await Sql.SeedAccountAsync();
        var subject = await Sql.SeedSubjectAsync();
        await Sql.AssignAsync(lecturer.Id, subject.Id);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            new LecturerSubjectRepository(Sql.NewContext()).AddAsync(new LecturerSubject { LecturerId = lecturer.Id, SubjectId = subject.Id }));
    }
}

public class QuestionRepositoryTests
{
    [SqlFact]
    public async Task GetBySubject_FiltersActive_AndOrdersById()
    {
        var subject = await Sql.SeedSubjectAsync();
        var q1 = await Sql.SeedQuestionAsync(subject.Id, "Một");
        await Sql.SeedQuestionAsync(subject.Id, "Hai (tắt)", active: false);
        var q3 = await Sql.SeedQuestionAsync(subject.Id, "Ba");
        await Sql.SeedQuestionAsync((await Sql.SeedSubjectAsync()).Id, "Của môn khác");
        var repo = new QuestionRepository(Sql.NewContext());

        Assert.Equal(new[] { q1.Id, q3.Id }, (await repo.GetBySubjectAsync(subject.Id, onlyActive: true)).Select(q => q.Id));
        Assert.Equal(3, (await repo.GetBySubjectAsync(subject.Id, onlyActive: false)).Count);
    }

    [SqlFact]
    public async Task Add_Update_GetById()
    {
        var subject = await Sql.SeedSubjectAsync();
        var repo = new QuestionRepository(Sql.NewContext());
        var question = new Question { SubjectId = subject.Id, Content = "Câu mới", ExpectedPoints = "a;b", IsActive = true };
        await repo.AddAsync(question);
        Assert.True(question.Id > 0);

        var ctx = Sql.NewContext();
        var repo2 = new QuestionRepository(ctx);
        var tracked = (await repo2.GetByIdAsync(question.Id))!;
        tracked.IsActive = false;
        await repo2.UpdateAsync(tracked);

        Assert.False((await new QuestionRepository(Sql.NewContext()).GetByIdAsync(question.Id))!.IsActive);
        Assert.Null(await repo.GetByIdAsync(int.MaxValue));
    }
}

public class ExamRepositoryTests
{
    [SqlFact]
    public async Task Add_SavesTheWholeGraph_InOneCall_AndGetDetailLoadsItBack()
    {
        var subject = await Sql.SeedSubjectAsync();
        var creator = await Sql.SeedAccountAsync("Admin");
        var qs = new[] { await Sql.SeedQuestionAsync(subject.Id, "Q1"), await Sql.SeedQuestionAsync(subject.Id, "Q2") };

        var session = new ExamSession
        {
            SubjectId = subject.Id, Title = "Phiên", StartTime = DateTime.Now.AddDays(1), SlotMinutes = 10, MainQuestionCount = 2,
            MaxFollowUps = 3, MaxFollowUpsPerQuestion = 1, AnswerSeconds = 30, Status = "Open", CreatedBy = creator.Id
        };
        var p = new ExamParticipant { StudentCode = "SE1", FullName = "A", Status = "Scheduled", SlotStart = session.StartTime, SlotEnd = session.StartTime.AddMinutes(10) };
        p.ParticipantQuestions.Add(new ParticipantQuestion { QuestionId = qs[0].Id, OrderNo = 1 });
        p.ParticipantQuestions.Add(new ParticipantQuestion { QuestionId = qs[1].Id, OrderNo = 2 });
        session.ExamParticipants.Add(p);

        await new ExamRepository(Sql.NewContext()).AddAsync(session);

        Assert.True(session.Id > 0 && p.Id > 0);
        var loaded = (await new ExamRepository(Sql.NewContext()).GetDetailAsync(session.Id))!;
        Assert.Equal(subject.Code, loaded.Subject.Code);
        var person = Assert.Single(loaded.ExamParticipants);
        Assert.Equal(new[] { "Q1", "Q2" }, person.ParticipantQuestions.OrderBy(x => x.OrderNo).Select(x => x.Question.Content));
    }

    [SqlFact]
    public async Task GetList_FiltersBySubjectIds_NewestFirst_WithParticipants()
    {
        var s1 = await Sql.SeedSubjectAsync();
        var s2 = await Sql.SeedSubjectAsync();
        var creator = await Sql.SeedAccountAsync("Admin");
        var q1 = await Sql.SeedQuestionAsync(s1.Id);
        var q2 = await Sql.SeedQuestionAsync(s2.Id);
        var a = await Sql.SeedSessionAsync(s1, creator, new[] { q1 }, "SE1", "SE2");
        await Sql.SeedSessionAsync(s2, creator, new[] { q2 }, "SE3");
        var repo = new ExamRepository(Sql.NewContext());

        var only1 = await repo.GetListAsync(new[] { s1.Id });
        Assert.Equal(a.Id, Assert.Single(only1).Id);
        Assert.Equal(2, only1[0].ExamParticipants.Count);
        Assert.Equal(s1.Code, only1[0].Subject.Code);

        var both = await repo.GetListAsync(new[] { s1.Id, s2.Id });
        Assert.Equal(2, both.Count);
        var all = await repo.GetListAsync(null);   // null = tất cả môn (Admin)
        Assert.True(all.Count >= 2);
        Assert.Empty(await repo.GetListAsync(Array.Empty<int>()));
    }

    [SqlFact]
    public async Task GetDetail_ReturnsTrackedEntities_SoServiceCanEditThenSave()
    {
        var subject = await Sql.SeedSubjectAsync();
        var creator = await Sql.SeedAccountAsync("Admin");
        var q = await Sql.SeedQuestionAsync(subject.Id);
        var seeded = await Sql.SeedSessionAsync(subject, creator, new[] { q }, "SE1");

        var ctx = Sql.NewContext();
        var repo = new ExamRepository(ctx);
        var tracked = (await repo.GetDetailAsync(seeded.Id))!;
        tracked.Status = "Closed";
        tracked.ExamParticipants.First().Status = "Completed";
        await repo.SaveAsync();

        var reloaded = (await new ExamRepository(Sql.NewContext()).GetDetailAsync(seeded.Id))!;
        Assert.Equal("Closed", reloaded.Status);
        Assert.Equal("Completed", reloaded.ExamParticipants.Single().Status);
    }

    [SqlFact]
    public async Task GetParticipantsByStudentCode_ReturnsTheirTurnsWithSessionAndSubject_InSlotOrder()
    {
        var subject = await Sql.SeedSubjectAsync();
        var creator = await Sql.SeedAccountAsync("Admin");
        var q = await Sql.SeedQuestionAsync(subject.Id);
        var code = "SV" + Sql.Unique();
        await Sql.SeedSessionAsync(subject, creator, new[] { q }, "SE-khac", code);
        await Sql.SeedSessionAsync(subject, creator, new[] { q }, code);

        var list = await new ExamRepository(Sql.NewContext()).GetParticipantsByStudentCodeAsync(code);

        Assert.Equal(2, list.Count);
        Assert.All(list, p => Assert.Equal(subject.Code, p.Session.Subject.Code));
        Assert.Equal(list.OrderBy(p => p.SlotStart).Select(p => p.Id), list.Select(p => p.Id));
        Assert.Empty(await new ExamRepository(Sql.NewContext()).GetParticipantsByStudentCodeAsync("khong-ton-tai"));
    }

    [SqlFact]
    public async Task SameStudentCodeTwiceInOneSession_IsRejectedByTheDatabase()
    {
        var subject = await Sql.SeedSubjectAsync();
        var creator = await Sql.SeedAccountAsync("Admin");
        var q = await Sql.SeedQuestionAsync(subject.Id);

        await Assert.ThrowsAsync<DbUpdateException>(() => Sql.SeedSessionAsync(subject, creator, new[] { q }, "SE1", "SE1"));
    }

    [SqlFact]
    public async Task OutOfRangeSessionSettings_AreRejectedByCheckConstraints()
    {
        var subject = await Sql.SeedSubjectAsync();
        var creator = await Sql.SeedAccountAsync("Admin");
        var session = new ExamSession
        {
            SubjectId = subject.Id, Title = "Sai", StartTime = DateTime.Now, SlotMinutes = 500, MainQuestionCount = 1,
            MaxFollowUps = 0, MaxFollowUpsPerQuestion = 0, AnswerSeconds = 60, Status = "Open", CreatedBy = creator.Id
        };

        await Assert.ThrowsAsync<DbUpdateException>(() => new ExamRepository(Sql.NewContext()).AddAsync(session));
    }
}

public class InterviewRepositoryTests
{
    private static async Task<(ExamSession session, Question question)> SeedAsync(params string[] codes)
    {
        var subject = await Sql.SeedSubjectAsync();
        var creator = await Sql.SeedAccountAsync("Admin");
        var q = await Sql.SeedQuestionAsync(subject.Id, "Câu hỏi phỏng vấn");
        return (await Sql.SeedSessionAsync(subject, creator, new[] { q }, codes.Length == 0 ? new[] { "SE1" } : codes), q);
    }

    [SqlFact]
    public async Task GetParticipant_LoadsSessionSubjectAndQuestions()
    {
        var (session, _) = await SeedAsync();
        var pid = (await new ExamRepository(Sql.NewContext()).GetDetailAsync(session.Id))!.ExamParticipants.Single().Id;

        var p = await new InterviewRepository(Sql.NewContext()).GetParticipantAsync(pid);

        Assert.NotNull(p);
        Assert.NotNull(p!.Session.Subject.Code);
        Assert.Equal("Câu hỏi phỏng vấn", Assert.Single(p.ParticipantQuestions).Question.Content);
        Assert.Null(await new InterviewRepository(Sql.NewContext()).GetParticipantAsync(int.MaxValue));
    }

    [SqlFact]
    public async Task AddTurn_IsOnlySavedOnSaveAsync_AndAnswerAndStatusAreSavedTogether()
    {
        var (session, q) = await SeedAsync();
        var pid = (await new ExamRepository(Sql.NewContext()).GetDetailAsync(session.Id))!.ExamParticipants.Single().Id;
        var ctx = Sql.NewContext();
        var repo = new InterviewRepository(ctx);

        Assert.False(await repo.HasTurnsAsync(pid));
        var participant = (await repo.GetParticipantAsync(pid))!;
        participant.Status = "InProgress";
        repo.AddTurn(new InterviewTurn { ParticipantId = pid, QuestionId = q.Id, Kind = "Main", Content = "Hỏi", AskedAt = DateTime.UtcNow });
        Assert.False(await new InterviewRepository(Sql.NewContext()).HasTurnsAsync(pid)); // chưa Save thì chưa có gì trong database
        await repo.SaveAsync();

        var repo2 = new InterviewRepository(Sql.NewContext());
        Assert.True(await repo2.HasTurnsAsync(pid));
        Assert.Equal("InProgress", (await repo2.GetParticipantAsync(pid))!.Status);
    }

    [SqlFact]
    public async Task GetTurns_AreOrderedById_GetTurnLoadsTheQuestion_AndTrackedEditsSave()
    {
        var (session, q) = await SeedAsync();
        var pid = (await new ExamRepository(Sql.NewContext()).GetDetailAsync(session.Id))!.ExamParticipants.Single().Id;
        var seedRepo = new InterviewRepository(Sql.NewContext());
        seedRepo.AddTurn(new InterviewTurn { ParticipantId = pid, QuestionId = q.Id, Kind = "Main", Content = "Một", AskedAt = DateTime.UtcNow });
        seedRepo.AddTurn(new InterviewTurn { ParticipantId = pid, QuestionId = q.Id, Kind = "FollowUp", Content = "Hai", Reason = "Mơ hồ", AskedAt = DateTime.UtcNow });
        await seedRepo.SaveAsync();

        var ctx = Sql.NewContext();
        var repo = new InterviewRepository(ctx);
        var turns = await repo.GetTurnsAsync(pid);
        Assert.Equal(new[] { "Một", "Hai" }, turns.Select(t => t.Content));

        var first = (await repo.GetTurnAsync(turns[0].Id))!;
        Assert.Equal("Câu hỏi phỏng vấn", first.Question.Content);
        first.Answer = "Trả lời";
        first.AnsweredAt = DateTime.UtcNow;
        first.TimedOut = true;
        await repo.SaveAsync();

        var saved = (await new InterviewRepository(Sql.NewContext()).GetTurnAsync(first.Id))!;
        Assert.Equal(("Trả lời", true), (saved.Answer, saved.TimedOut));
        Assert.Null(await repo.GetTurnAsync(int.MaxValue));
    }

    [SqlFact]
    public async Task Turn_WithAnInvalidKind_IsRejectedByTheDatabase()
    {
        var (session, q) = await SeedAsync();
        var pid = (await new ExamRepository(Sql.NewContext()).GetDetailAsync(session.Id))!.ExamParticipants.Single().Id;
        var repo = new InterviewRepository(Sql.NewContext());
        repo.AddTurn(new InterviewTurn { ParticipantId = pid, QuestionId = q.Id, Kind = "Bậy", Content = "x", AskedAt = DateTime.UtcNow });

        await Assert.ThrowsAsync<DbUpdateException>(() => repo.SaveAsync());
    }
}

public class VoiceRepositoryTests
{
    private static Voice NewVoice(int createdBy, string? name = null) =>
        new() { Name = name ?? "Giọng " + Sql.Unique(), FileName = "clip.wav", Denoise = true, Clip = new byte[] { 9, 8, 7 }, CreatedBy = createdBy };

    [SqlFact]
    public async Task Add_GetByName_ReturnsTheClip()
    {
        var admin = await Sql.SeedAccountAsync("Admin");
        var voice = NewVoice(admin.Id);
        await new VoiceRepository(Sql.NewContext()).AddAsync(voice);

        var loaded = await new VoiceRepository(Sql.NewContext()).GetByNameAsync(voice.Name);

        Assert.Equal(new byte[] { 9, 8, 7 }, loaded!.Clip);
        Assert.Null(await new VoiceRepository(Sql.NewContext()).GetByNameAsync("Không có giọng này"));
    }

    [SqlFact]
    public async Task GetAll_ListsVoices_WithoutLoadingTheHeavyClipColumn()
    {
        var admin = await Sql.SeedAccountAsync("Admin");
        var voice = NewVoice(admin.Id);
        await new VoiceRepository(Sql.NewContext()).AddAsync(voice);

        var all = await new VoiceRepository(Sql.NewContext()).GetAllAsync();

        var item = all.Single(v => v.Name == voice.Name);
        Assert.Empty(item.Clip);
        Assert.Equal(all.OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase).Select(v => v.Id), all.Select(v => v.Id));
    }

    [SqlFact]
    public async Task Add_DuplicateName_IsRejected()
    {
        var admin = await Sql.SeedAccountAsync("Admin");
        var voice = NewVoice(admin.Id);
        await new VoiceRepository(Sql.NewContext()).AddAsync(voice);

        await Assert.ThrowsAsync<DbUpdateException>(() => new VoiceRepository(Sql.NewContext()).AddAsync(NewVoice(admin.Id, voice.Name)));
    }

    [SqlFact]
    public async Task Remove_DeletesTheVoice_AndSubjectsUsingItGoBackToDefault()
    {
        var admin = await Sql.SeedAccountAsync("Admin");
        var voice = NewVoice(admin.Id);
        await new VoiceRepository(Sql.NewContext()).AddAsync(voice);
        var subject = await Sql.SeedSubjectAsync();
        var ctx = Sql.NewContext();
        var tracked = await ctx.Subjects.FirstAsync(s => s.Id == subject.Id);
        tracked.TtsVoice = voice.Name;
        await ctx.SaveChangesAsync();

        Assert.True(await new VoiceRepository(Sql.NewContext()).RemoveAsync(voice.Name));

        Assert.Null(await new VoiceRepository(Sql.NewContext()).GetByNameAsync(voice.Name));
        Assert.Null((await new SubjectRepository(Sql.NewContext()).GetByIdAsync(subject.Id))!.TtsVoice);
        Assert.False(await new VoiceRepository(Sql.NewContext()).RemoveAsync(voice.Name));
    }
}
