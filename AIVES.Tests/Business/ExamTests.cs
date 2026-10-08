using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Models;
using AIVES.Business.Services;
using AIVES.DataAccess.Entities;
using AIVES.Tests.Support;
using Xunit;

namespace AIVES.Tests.Business;

// ====== chọn bộ câu hỏi, ngân hàng câu hỏi, phiên thi và lịch thi ======

public class QuestionAllocatorTests
{
    [Fact]
    public void EachStudent_GetsTheRequestedNumberOfDistinctQuestions()
    {
        var sets = QuestionAllocator.Allocate(Enumerable.Range(1, 8).ToList(), students: 10, perStudent: 3, new Random(1));

        Assert.Equal(10, sets.Count);
        Assert.All(sets, s =>
        {
            Assert.Equal(3, s.Length);
            Assert.Equal(3, s.Distinct().Count());
        });
    }

    [Fact]
    public void TwoStudentsInARow_NeverShareAQuestion_WhenThePoolIsBigEnough()
    {
        var sets = QuestionAllocator.Allocate(Enumerable.Range(1, 8).ToList(), students: 30, perStudent: 3, new Random(7));

        for (var i = 1; i < sets.Count; i++)
            Assert.Empty(sets[i].Intersect(sets[i - 1]));
    }

    [Fact]
    public void Questions_AreUsedAboutEvenly()
    {
        var sets = QuestionAllocator.Allocate(Enumerable.Range(1, 8).ToList(), students: 20, perStudent: 3, new Random(3));

        var counts = sets.SelectMany(s => s).GroupBy(x => x).Select(g => g.Count()).ToList();

        Assert.Equal(8, counts.Count);                 // câu nào cũng được dùng
        Assert.True(counts.Max() - counts.Min() <= 2); // không câu nào bị dùng quá nhiều so với câu khác
    }

    [Fact]
    public void WhenThePoolIsTooSmall_QuestionsAreReused_ButStillDistinctPerStudent()
    {
        // 4 câu, mỗi người 3 câu: bắt buộc phải trùng với người trước, nhưng một người không bao giờ nhận cùng câu hai lần
        var sets = QuestionAllocator.Allocate(new[] { 1, 2, 3, 4 }, students: 6, perStudent: 3, new Random(2));

        Assert.All(sets, s => Assert.Equal(3, s.Distinct().Count()));
    }

    [Fact]
    public void OnlyQuestionsFromThePoolAreUsed()
    {
        var pool = new[] { 11, 22, 33, 44, 55 };
        var sets = QuestionAllocator.Allocate(pool, 5, 2, new Random(5));
        Assert.All(sets.SelectMany(s => s), id => Assert.Contains(id, pool));
    }

    [Fact]
    public void NoStudents_GivesNoSets()
    {
        Assert.Empty(QuestionAllocator.Allocate(new[] { 1, 2, 3 }, 0, 2, new Random(1)));
    }
}

public class QuestionServiceTests
{
    private readonly TestDb _db = new();

    private QuestionService Service() =>
        new(_db.QuestionRepo, new AssignmentService(_db.AccountRepo, _db.SubjectRepo, _db.LecturerSubjectRepo));

    private (Account lecturer, Subject subject) Setup()
    {
        var lecturer = _db.AddAccount(Roles.Lecturer);
        var subject = _db.AddSubject("PRN222");
        _db.Assign(lecturer, subject);
        return (lecturer, subject);
    }

    [Fact]
    public async Task Add_TrimsText_AndStoresBlankPointsAsNull()
    {
        var (lecturer, subject) = Setup();

        var result = await Service().AddAsync(lecturer.Id, new AddQuestionRequest(subject.Id, "  DI là gì?  ", "   "));

        Assert.True(result.Succeeded);
        var saved = Assert.Single(_db.Questions);
        Assert.Equal("DI là gì?", saved.Content);
        Assert.Null(saved.ExpectedPoints);
        Assert.True(saved.IsActive);
    }

    [Fact]
    public async Task Add_KeepsExpectedPoints()
    {
        var (lecturer, subject) = Setup();
        await Service().AddAsync(lecturer.Id, new AddQuestionRequest(subject.Id, "DI là gì?", " AddScoped;AddSingleton "));
        Assert.Equal("AddScoped;AddSingleton", _db.Questions.Single().ExpectedPoints);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Add_BlankContent_IsRejected(string content)
    {
        var (lecturer, subject) = Setup();
        Assert.False((await Service().AddAsync(lecturer.Id, new AddQuestionRequest(subject.Id, content, null))).Succeeded);
        Assert.Empty(_db.Questions);
    }

    [Fact]
    public async Task Add_TooLong_IsRejected()
    {
        var (lecturer, subject) = Setup();
        Assert.False((await Service().AddAsync(lecturer.Id, new AddQuestionRequest(subject.Id, new string('a', 1001), null))).Succeeded);
        Assert.False((await Service().AddAsync(lecturer.Id, new AddQuestionRequest(subject.Id, "ok", new string('b', 1001)))).Succeeded);
    }

    [Fact]
    public async Task Add_LecturerNotAssignedToTheSubject_IsForbidden()
    {
        var (_, subject) = Setup();
        var stranger = _db.AddAccount(Roles.Lecturer);

        var result = await Service().AddAsync(stranger.Id, new AddQuestionRequest(subject.Id, "Câu hỏi", null));

        Assert.Equal(ServiceErrorType.Forbidden, result.ErrorType);
        Assert.Empty(_db.Questions);
    }

    [Fact]
    public async Task GetBySubject_IncludesInactiveQuestions_AndChecksAccess()
    {
        var (lecturer, subject) = Setup();
        _db.AddQuestion(subject, "A");
        _db.AddQuestion(subject, "B", active: false);
        var stranger = _db.AddAccount(Roles.Lecturer);

        var mine = await Service().GetBySubjectAsync(lecturer.Id, subject.Id);
        var denied = await Service().GetBySubjectAsync(stranger.Id, subject.Id);

        Assert.Equal(2, mine.Data!.Count);
        Assert.Contains(mine.Data, q => !q.IsActive);
        Assert.Equal(ServiceErrorType.Forbidden, denied.ErrorType);
    }

    [Fact]
    public async Task SetActive_TogglesTheQuestion()
    {
        var (lecturer, subject) = Setup();
        var q = _db.AddQuestion(subject);

        Assert.True((await Service().SetActiveAsync(lecturer.Id, q.Id, false)).Succeeded);
        Assert.False(q.IsActive);
        Assert.True((await Service().SetActiveAsync(lecturer.Id, q.Id, true)).Succeeded);
        Assert.True(q.IsActive);
    }

    [Fact]
    public async Task SetActive_UnknownQuestion_IsNotFound_AndStrangerIsForbidden()
    {
        var (lecturer, subject) = Setup();
        var q = _db.AddQuestion(subject);
        var stranger = _db.AddAccount(Roles.Lecturer);

        Assert.Equal(ServiceErrorType.NotFound, (await Service().SetActiveAsync(lecturer.Id, 999, false)).ErrorType);
        Assert.Equal(ServiceErrorType.Forbidden, (await Service().SetActiveAsync(stranger.Id, q.Id, false)).ErrorType);
        Assert.True(q.IsActive);
    }
}

public class ExamServiceTests
{
    private readonly TestDb _db = new();
    private readonly Account _admin;
    private readonly Account _lecturer;
    private readonly Subject _subject;

    public ExamServiceTests()
    {
        _admin = _db.AddAccount(Roles.Admin);
        _lecturer = _db.AddAccount(Roles.Lecturer);
        _subject = _db.AddSubject("PRN222");
        _db.Assign(_lecturer, _subject);
        _db.AddQuestions(_subject, 8);
        // Thí sinh phải có tài khoản Sinh viên: tạo sẵn SE1 đến SE200 cho các test dưới đây
        for (var i = 1; i <= 200; i++) _db.AddAccount(Roles.Student, studentCode: $"SE{i}");
    }

    private ExamService Service() =>
        new(_db.ExamRepo, _db.QuestionRepo, _db.AccountRepo, _db.SubjectRepo,
            new AssignmentService(_db.AccountRepo, _db.SubjectRepo, _db.LecturerSubjectRepo));

    private CreateExamRequest Request(Func<CreateExamRequest, CreateExamRequest>? change = null)
    {
        var request = new CreateExamRequest(_subject.Id, "Vấn đáp giữa kỳ", DateTime.Now.AddDays(2), 10, 3, 4, 2, 60,
            "SE1; Nguyễn A\nSE2; Trần B\nSE3; Lê C");
        return change is null ? request : change(request);
    }

    // ---- Tạo phiên thi
    [Fact]
    public async Task Create_Valid_SavesSessionWithSettings()
    {
        var start = DateTime.Now.AddDays(2);

        var result = await Service().CreateAsync(_lecturer.Id, Request(r => r with { StartTime = start }));

        Assert.True(result.Succeeded);
        var session = Assert.Single(_db.Sessions);
        Assert.Equal(result.Data, session.Id);
        Assert.Equal(_subject.Id, session.SubjectId);
        Assert.Equal("Vấn đáp giữa kỳ", session.Title);
        Assert.Equal(start, session.StartTime);
        Assert.Equal((10, 3, 4, 2, 60), (session.SlotMinutes, session.MainQuestionCount, session.MaxFollowUps, session.MaxFollowUpsPerQuestion, session.AnswerSeconds));
        Assert.Equal(ExamStatuses.Open, session.Status);
        Assert.Equal(_lecturer.Id, session.CreatedBy);
    }

    [Fact]
    public async Task Create_GivesEachStudentAConsecutiveTimeSlot_InListOrder()
    {
        var start = new DateTime(2030, 1, 1, 8, 0, 0);

        await Service().CreateAsync(_lecturer.Id, Request(r => r with { StartTime = start, SlotMinutes = 15 }));

        var people = _db.Sessions.Single().ExamParticipants.OrderBy(p => p.SlotStart).ToList();
        Assert.Equal(new[] { "SE1", "SE2", "SE3" }, people.Select(p => p.StudentCode));
        Assert.Equal(new[] { start, start.AddMinutes(15), start.AddMinutes(30) }, people.Select(p => p.SlotStart));
        Assert.Equal(new[] { start.AddMinutes(15), start.AddMinutes(30), start.AddMinutes(45) }, people.Select(p => p.SlotEnd));
        Assert.All(people, p => Assert.Equal(ParticipantStatuses.Scheduled, p.Status));
    }

    [Fact]
    public async Task Create_AllocatesDistinctOrderedQuestionsToEveryStudent_WithoutOverlapBetweenNeighbours()
    {
        await Service().CreateAsync(_lecturer.Id, Request());

        var people = _db.Sessions.Single().ExamParticipants.OrderBy(p => p.SlotStart).ToList();
        Assert.All(people, p =>
        {
            Assert.Equal(3, p.ParticipantQuestions.Count);
            Assert.Equal(new[] { 1, 2, 3 }, p.ParticipantQuestions.Select(q => q.OrderNo).OrderBy(n => n));
            Assert.Equal(3, p.ParticipantQuestions.Select(q => q.QuestionId).Distinct().Count());
        });
        for (var i = 1; i < people.Count; i++)
            Assert.Empty(people[i].ParticipantQuestions.Select(q => q.QuestionId)
                .Intersect(people[i - 1].ParticipantQuestions.Select(q => q.QuestionId)));
    }

    [Fact]
    public async Task Create_OnlyUsesActiveQuestions()
    {
        foreach (var q in _db.Questions.Take(4)) q.IsActive = false; // còn 4 câu đang dùng

        await Service().CreateAsync(_lecturer.Id, Request(r => r with { MainQuestionCount = 3 }));

        var used = _db.Sessions.Single().ExamParticipants.SelectMany(p => p.ParticipantQuestions).Select(q => q.QuestionId).Distinct();
        Assert.All(used, id => Assert.True(_db.Questions.Single(q => q.Id == id).IsActive));
    }

    [Theory]
    [InlineData("SE1; Nguyễn A", 1, "SE1", "Nguyễn A")]
    [InlineData("SE1, Nguyễn A", 1, "SE1", "Nguyễn A")]
    [InlineData("SE1\tNguyễn A", 1, "SE1", "Nguyễn A")]
    [InlineData("SE1 Nguyễn Văn A", 1, "SE1", "Nguyễn Văn A")]
    [InlineData("  SE1 ;  Nguyễn A  \n\n", 1, "SE1", "Nguyễn A")]
    public async Task Create_StudentList_AcceptsSeveralSeparators_AndIgnoresBlankLines(string list, int count, string code, string name)
    {
        var result = await Service().CreateAsync(_lecturer.Id, Request(r => r with { StudentList = list }));

        Assert.True(result.Succeeded);
        var person = Assert.Single(_db.Sessions.Single().ExamParticipants);
        Assert.Equal(count, _db.Sessions.Single().ExamParticipants.Count);
        Assert.Equal((code, name), (person.StudentCode, person.FullName));
    }

    [Fact]
    public async Task Create_EveryStudentCodeMustBelongToAStudentAccount()
    {
        // SE999 không có tài khoản; giảng viên, admin không có MSSV nên cũng không thể là thí sinh
        var result = await Service().CreateAsync(_lecturer.Id, Request(r => r with { StudentList = "SE1; Nguyễn A\nSE999; Không Có" }));

        Assert.False(result.Succeeded);
        Assert.Contains("SE999", result.Error);
        Assert.Empty(_db.Sessions);
    }

    [Fact]
    public async Task Create_StudentCodeCheck_IgnoresCase()
    {
        Assert.True((await Service().CreateAsync(_lecturer.Id, Request(r => r with { StudentList = "se1; Nguyễn A" }))).Succeeded);
    }

    [Theory]
    [InlineData("")]                        // không có sinh viên
    [InlineData("   \n  ")]
    [InlineData("SE1")]                     // thiếu họ tên
    [InlineData("SE1; Nguyễn A\nSE1; Trùng mã")]
    [InlineData("SE1; Nguyễn A\nse1; Trùng mã khác hoa thường")]
    public async Task Create_BadStudentList_IsRejected(string list)
    {
        var result = await Service().CreateAsync(_lecturer.Id, Request(r => r with { StudentList = list }));

        Assert.False(result.Succeeded);
        Assert.Empty(_db.Sessions);
    }

    [Fact]
    public async Task Create_MoreThan200Students_IsRejected()
    {
        var list = string.Join("\n", Enumerable.Range(1, 201).Select(i => $"SE{i}; Sinh viên {i}"));
        Assert.False((await Service().CreateAsync(_lecturer.Id, Request(r => r with { StudentList = list }))).Succeeded);
        Assert.True((await Service().CreateAsync(_lecturer.Id, Request(r => r with { StudentList = string.Join("\n", Enumerable.Range(1, 200).Select(i => $"SE{i}; SV {i}")) }))).Succeeded);
    }

    [Theory]
    [InlineData(0, 3, 4, 2, 60)]    // khung giờ phải 1..120
    [InlineData(121, 3, 4, 2, 60)]
    [InlineData(10, 0, 4, 2, 60)]   // số câu chính 1..20
    [InlineData(10, 21, 4, 2, 60)]
    [InlineData(10, 3, -1, 2, 60)]  // câu đào sâu mỗi thí sinh 0..40
    [InlineData(10, 3, 41, 2, 60)]
    [InlineData(10, 3, 4, -1, 60)]  // lượt hỏi xoáy mỗi câu 0..5
    [InlineData(10, 3, 4, 6, 60)]
    [InlineData(10, 3, 4, 2, 9)]    // thời gian trả lời 10..600 giây
    [InlineData(10, 3, 4, 2, 601)]
    public async Task Create_NumbersOutOfRange_AreRejected(int slot, int main, int followUps, int perQuestion, int seconds)
    {
        var result = await Service().CreateAsync(_lecturer.Id, Request(r => r with
        {
            SlotMinutes = slot, MainQuestionCount = main, MaxFollowUps = followUps, MaxFollowUpsPerQuestion = perQuestion, AnswerSeconds = seconds
        }));

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Empty(_db.Sessions);
    }

    [Theory]
    [InlineData(1, 1, 0, 0, 10)]     // biên dưới
    [InlineData(120, 20, 40, 5, 600)] // biên trên
    public async Task Create_BoundaryNumbers_AreAccepted(int slot, int main, int followUps, int perQuestion, int seconds)
    {
        _db.AddQuestions(_subject, 20);

        var result = await Service().CreateAsync(_lecturer.Id, Request(r => r with
        {
            SlotMinutes = slot, MainQuestionCount = main, MaxFollowUps = followUps, MaxFollowUpsPerQuestion = perQuestion, AnswerSeconds = seconds
        }));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Create_BlankOrTooLongTitle_IsRejected()
    {
        Assert.False((await Service().CreateAsync(_lecturer.Id, Request(r => r with { Title = "  " }))).Succeeded);
        Assert.False((await Service().CreateAsync(_lecturer.Id, Request(r => r with { Title = new string('a', 151) }))).Succeeded);
    }

    [Fact]
    public async Task Create_StartInThePast_IsRejected()
    {
        var result = await Service().CreateAsync(_lecturer.Id, Request(r => r with { StartTime = DateTime.Now.AddDays(-1) }));

        Assert.False(result.Succeeded);
        Assert.Empty(_db.Sessions);
    }

    [Fact]
    public async Task Create_NotEnoughActiveQuestions_IsRejected()
    {
        var result = await Service().CreateAsync(_lecturer.Id, Request(r => r with { MainQuestionCount = 9 })); // ngân hàng chỉ có 8 câu

        Assert.False(result.Succeeded);
        Assert.Empty(_db.Sessions);
    }

    [Fact]
    public async Task Create_UnknownSubject_IsNotFound_StrangerIsForbidden_InactiveSubjectIsRejected()
    {
        Assert.Equal(ServiceErrorType.NotFound, (await Service().CreateAsync(_lecturer.Id, Request(r => r with { SubjectId = 999 }))).ErrorType);

        var stranger = _db.AddAccount(Roles.Lecturer);
        Assert.Equal(ServiceErrorType.Forbidden, (await Service().CreateAsync(stranger.Id, Request())).ErrorType);

        _subject.IsActive = false;
        Assert.False((await Service().CreateAsync(_admin.Id, Request())).Succeeded);
    }

    [Fact]
    public async Task Create_Admin_CanCreateForAnySubject()
    {
        Assert.True((await Service().CreateAsync(_admin.Id, Request())).Succeeded);
    }

    // ---- Danh sách, chi tiết
    [Fact]
    public async Task GetList_AdminSeesAll_LecturerOnlyTheirSubjects_LockedSeesNothing()
    {
        var otherSubject = _db.AddSubject("SWP391");
        _db.AddQuestions(otherSubject, 5);
        var otherLecturer = _db.AddAccount(Roles.Lecturer);
        _db.Assign(otherLecturer, otherSubject);
        await Service().CreateAsync(_lecturer.Id, Request());
        await Service().CreateAsync(otherLecturer.Id, Request(r => r with { SubjectId = otherSubject.Id, MainQuestionCount = 2 }));

        Assert.Equal(2, (await Service().GetListAsync(_admin.Id)).Count);
        var mine = await Service().GetListAsync(_lecturer.Id);
        Assert.Equal("PRN222", Assert.Single(mine).SubjectCode);
        Assert.Equal(3, mine[0].ParticipantCount);

        _lecturer.IsActive = false;
        Assert.Empty(await Service().GetListAsync(_lecturer.Id));
    }

    [Fact]
    public async Task GetSubjectsForExam_OnlyActiveSubjectsTheUserMayUse()
    {
        _db.AddSubject("SWP391");                    // không được phân công
        _db.AddSubject("PRN211", active: false);

        Assert.Equal(new[] { "PRN222", "SWP391" }, (await Service().GetSubjectsForExamAsync(_admin.Id)).Select(s => s.Code));
        Assert.Equal("PRN222", Assert.Single(await Service().GetSubjectsForExamAsync(_lecturer.Id)).Code);
    }

    [Fact]
    public async Task GetDetail_ReturnsParticipantsInSlotOrder_WithTheirQuestionsInOrder()
    {
        var created = await Service().CreateAsync(_lecturer.Id, Request());

        var result = await Service().GetDetailAsync(_lecturer.Id, created.Data);

        Assert.True(result.Succeeded);
        Assert.Equal("PRN222", result.Data!.SubjectCode);
        Assert.Equal(new[] { "SE1", "SE2", "SE3" }, result.Data.Participants.Select(p => p.StudentCode));
        Assert.All(result.Data.Participants, p => Assert.Equal(3, p.Questions.Count));
    }

    [Fact]
    public async Task GetDetail_UnknownIsNotFound_StrangerIsForbidden()
    {
        var created = await Service().CreateAsync(_lecturer.Id, Request());
        var stranger = _db.AddAccount(Roles.Lecturer);

        Assert.Equal(ServiceErrorType.NotFound, (await Service().GetDetailAsync(_lecturer.Id, 999)).ErrorType);
        Assert.Equal(ServiceErrorType.Forbidden, (await Service().GetDetailAsync(stranger.Id, created.Data)).ErrorType);
    }

    // ---- Đóng/mở, chọn lại câu hỏi
    [Fact]
    public async Task SetOpen_ClosesAndReopens_WithAccessCheck()
    {
        var created = await Service().CreateAsync(_lecturer.Id, Request());
        var session = _db.Sessions.Single();
        var stranger = _db.AddAccount(Roles.Lecturer);

        Assert.True((await Service().SetOpenAsync(_lecturer.Id, created.Data, false)).Succeeded);
        Assert.Equal(ExamStatuses.Closed, session.Status);
        Assert.True((await Service().SetOpenAsync(_lecturer.Id, created.Data, true)).Succeeded);
        Assert.Equal(ExamStatuses.Open, session.Status);

        Assert.Equal(ServiceErrorType.Forbidden, (await Service().SetOpenAsync(stranger.Id, created.Data, false)).ErrorType);
        Assert.Equal(ServiceErrorType.NotFound, (await Service().SetOpenAsync(_lecturer.Id, 999, false)).ErrorType);
    }

    [Fact]
    public async Task Reallocate_GivesEveryoneAFreshValidSet()
    {
        var created = await Service().CreateAsync(_lecturer.Id, Request());

        var result = await Service().ReallocateAsync(_lecturer.Id, created.Data);

        Assert.True(result.Succeeded);
        var people = _db.Sessions.Single().ExamParticipants.OrderBy(p => p.SlotStart).ToList();
        Assert.All(people, p => Assert.Equal(3, p.ParticipantQuestions.Count));
        for (var i = 1; i < people.Count; i++)
            Assert.Empty(people[i].ParticipantQuestions.Select(q => q.QuestionId).Intersect(people[i - 1].ParticipantQuestions.Select(q => q.QuestionId)));
    }

    [Theory]
    [InlineData(ParticipantStatuses.InProgress)]
    [InlineData(ParticipantStatuses.Completed)]
    public async Task Reallocate_AfterSomeoneStarted_IsRejected(string status)
    {
        var created = await Service().CreateAsync(_lecturer.Id, Request());
        _db.Sessions.Single().ExamParticipants.First().Status = status;

        Assert.False((await Service().ReallocateAsync(_lecturer.Id, created.Data)).Succeeded);
    }

    [Fact]
    public async Task Reallocate_WhenBankShrankBelowTheNeededCount_IsRejected()
    {
        var created = await Service().CreateAsync(_lecturer.Id, Request());
        foreach (var q in _db.Questions.Skip(2)) q.IsActive = false; // còn 2 câu < 3 câu chính

        Assert.False((await Service().ReallocateAsync(_lecturer.Id, created.Data)).Succeeded);
    }
}
