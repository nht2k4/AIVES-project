using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AIVES.Ass2.Razor.Infrastructure;
using AIVES.Ass2.Razor.Pages.Exams;
using AIVES.Business.Models;
using AIVES.Business.Services;
using AIVES.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.SignalR;
using Xunit;

namespace TV6.Verify;

// PageModel + service thật, repository trong bộ nhớ; không cần SQL Server.
public class CreateChecks
{
    private readonly TestDb db = new();
    private readonly RecordingHub hub = new();

    private CreateModel CreatePage(out int subjectId)
    {
        var lecturer = db.AddAccount(Roles.Lecturer);
        var subject = db.AddSubject("PRN222");
        db.Assign(lecturer, subject);
        db.AddQuestions(subject, 8);
        var stopped = db.AddSubject("STOP", active: false);
        db.Assign(lecturer, stopped);
        db.AddSubject("OTHER");
        subjectId = subject.Id;
        var service = new ExamService(db.ExamRepo, db.QuestionRepo, db.AccountRepo, db.SubjectRepo,
            new AssignmentService(db.AccountRepo, db.SubjectRepo, db.LecturerSubjectRepo));
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, lecturer.Id.ToString()),
                new Claim(ClaimTypes.Role, Roles.Lecturer)
            }, "Test"))
        };
        var page = new CreateModel(service, hub)
        {
            PageContext = new PageContext { HttpContext = http },
            TempData = new TempDataDictionary(http, new MemoryTempData()),
            Input = new CreateModel.InputModel
            {
                SubjectId = subject.Id, Title = "Thi thực hành",
                StartTime = DateTime.Today.AddDays(1).AddHours(8),
                StudentList = "SE1; Nguyễn An\nSE2; Trần Bình\nSE3; Lê Chi"
            }
        };
        hub.Client.OnSend = () => Assert.Single(db.Sessions);
        return page;
    }

    [Fact]
    public async Task Get_OnlyOffersAssignedActiveSubjects_AndTomorrowMorning()
    {
        var page = CreatePage(out var subjectId);
        await page.OnGetAsync();
        Assert.Equal(subjectId, Assert.Single(page.Subjects).Id);
        Assert.Equal(DateTime.Today.AddDays(1).AddHours(8), page.Input.StartTime);
        Assert.Equal((10, 3, 4, 2, 60), (page.Input.SlotMinutes, page.Input.MainQuestionCount,
            page.Input.MaxFollowUps, page.Input.MaxFollowUpsPerQuestion, page.Input.AnswerSeconds));
    }

    [Fact]
    public async Task ValidPost_SavesSchedule_ThenNotifies_AndRedirects()
    {
        var page = CreatePage(out _);
        var response = Assert.IsType<RedirectToPageResult>(await page.OnPostAsync());
        Assert.Equal("./Index", response.PageName);
        var session = Assert.Single(db.Sessions);
        var people = session.ExamParticipants.OrderBy(p => p.SlotStart).ToArray();
        Assert.Equal(3, people.Length);
        for (var i = 0; i < people.Length; i++)
        {
            Assert.Equal(page.Input.StartTime!.Value.AddMinutes(i * 10), people[i].SlotStart);
            Assert.Equal(people[i].SlotStart.AddMinutes(10), people[i].SlotEnd);
            Assert.Equal(3, people[i].ParticipantQuestions.Count);
        }
        var notification = Assert.Single(hub.Client.Messages);
        Assert.Equal("changed", notification.Method);
        Assert.Equal("exams", notification.Args[0]);
        Assert.Equal("Đã tạo phiên thi.", page.TempData["Success"]);
    }

    [Theory]
    [InlineData("SE1")]
    [InlineData("SE1; An\nse1; Bình")]
    public async Task BadStudentList_KeepsInputAndOptions_WithoutSavingOrNotifying(string students)
    {
        var page = CreatePage(out var subjectId);
        page.Input.StudentList = students;
        Assert.IsType<PageResult>(await page.OnPostAsync());
        Assert.False(page.ModelState.IsValid);
        Assert.Equal(students, page.Input.StudentList);
        Assert.Equal(subjectId, Assert.Single(page.Subjects).Id);
        Assert.Empty(db.Sessions);
        Assert.Empty(hub.Client.Messages);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ForgedSubject_MapsForbiddenOrNotFound_WithoutNotification(bool missing)
    {
        var page = CreatePage(out _);
        page.Input.SubjectId = missing ? int.MaxValue : db.Subjects.Single(s => s.Code == "OTHER").Id;
        var result = await page.OnPostAsync();
        if (missing) Assert.IsType<NotFoundResult>(result);
        else Assert.IsType<ForbidResult>(result);
        Assert.Empty(db.Sessions);
        Assert.Empty(hub.Client.Messages);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(121)]
    [InlineData(null)]
    public async Task InvalidForm_StopsBeforeSaving(int? minutes)
    {
        var page = CreatePage(out _);
        page.Input.SlotMinutes = minutes;
        var errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(page.Input, new ValidationContext(page.Input), errors, true));
        // MVC thực hiện bước này trước khi gọi handler trong ứng dụng thật.
        foreach (var error in errors)
            page.ModelState.AddModelError("Input." + error.MemberNames.First(), error.ErrorMessage!);
        Assert.IsType<PageResult>(await page.OnPostAsync());
        Assert.Empty(db.Sessions);
        Assert.Empty(hub.Client.Messages);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BusinessFailure_PastTimeOrInsufficientQuestions_DoesNotNotify(bool past)
    {
        var page = CreatePage(out _);
        if (past) page.Input.StartTime = DateTime.Now.AddDays(-1);
        else db.Questions.Clear();
        Assert.IsType<PageResult>(await page.OnPostAsync());
        Assert.False(page.ModelState.IsValid);
        Assert.Empty(db.Sessions);
        Assert.Empty(hub.Client.Messages);
    }

    private sealed class MemoryTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private sealed class RecordingHub : IHubContext<LiveHub>
    {
        public RecordingClient Client { get; } = new();
        public IHubClients Clients => new BroadcastClients(Client);
        public IGroupManager Groups => throw new NotSupportedException();
    }

    private sealed class RecordingClient : IClientProxy
    {
        public List<(string Method, object?[] Args)> Messages { get; } = new();
        public Action? OnSend { get; set; }
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            OnSend?.Invoke();
            Messages.Add((method, args));
            return Task.CompletedTask;
        }
    }

    private sealed class BroadcastClients(IClientProxy client) : IHubClients
    {
        public IClientProxy All => client;
        public IClientProxy AllExcept(IReadOnlyList<string> ids) => throw new NotSupportedException();
        public IClientProxy Client(string id) => throw new NotSupportedException();
        public IClientProxy Clients(IReadOnlyList<string> ids) => throw new NotSupportedException();
        public IClientProxy Group(string name) => throw new NotSupportedException();
        public IClientProxy GroupExcept(string name, IReadOnlyList<string> ids) => throw new NotSupportedException();
        public IClientProxy Groups(IReadOnlyList<string> names) => throw new NotSupportedException();
        public IClientProxy User(string id) => throw new NotSupportedException();
        public IClientProxy Users(IReadOnlyList<string> ids) => throw new NotSupportedException();
    }
}
