using System.Security.Claims;
using AIVES.Ass2.Razor.Pages.Exams;
using AIVES.Business.Models;
using AIVES.Business.Services;
using AIVES.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Xunit;

namespace TV6.Verify;

public class DetailsChecks
{
    private readonly TestDb db = new();
    private readonly CreateChecks.RecordingHub hub = new();

    private DetailsModel Page(out int id, bool assigned = true)
    {
        var lecturer = db.AddAccount(Roles.Lecturer);
        var subject = db.AddSubject("PRN222");
        if (assigned) db.Assign(lecturer, subject);
        var session = db.AddSession(subject, lecturer, studentCodes: new[] { "SE1", "SE2" });
        db.AddQuestions(subject, 5);
        id = session.Id;
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
        return new DetailsModel(service, hub)
        {
            PageContext = new PageContext { HttpContext = http },
            TempData = new TempDataDictionary(http, new CreateChecks.MemoryTempData())
        };
    }

    [Fact]
    public async Task Get_LoadsScheduleAndQuestions_WithoutSendingEvents()
    {
        var page = Page(out var id);
        Assert.IsType<PageResult>(await page.OnGetAsync(id));
        Assert.Equal(2, page.Exam.Participants.Count);
        Assert.All(page.Exam.Participants, p => Assert.Equal(2, p.Questions.Count));
        Assert.True(page.CanReallocate);
        Assert.Empty(hub.Client.Messages);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetOpen_SavesStatusBeforeNotifyingBothTopics(bool open)
    {
        var page = Page(out var id);
        var session = Assert.Single(db.Sessions);
        session.Status = open ? ExamStatuses.Closed : ExamStatuses.Open;
        hub.Client.OnSend = () => Assert.Equal(open ? ExamStatuses.Open : ExamStatuses.Closed, session.Status);
        var redirect = Assert.IsType<RedirectToPageResult>(await page.OnPostSetOpenAsync(id, open));
        Assert.Equal(id, redirect.RouteValues!["id"]);
        Assert.Equal(new[] { "exams", $"exam-{id}" }, hub.Client.Messages.Select(m => (string)m.Args[0]!));
        Assert.All(hub.Client.Messages, m => Assert.Equal("changed", m.Method));
    }

    [Theory]
    [InlineData("get", false)]
    [InlineData("close", false)]
    [InlineData("reallocate", false)]
    [InlineData("get", true)]
    [InlineData("close", true)]
    [InlineData("reallocate", true)]
    public async Task UnauthorizedOrMissingSession_RejectsEveryHandler(string action, bool missing)
    {
        var page = Page(out var id, assigned: false);
        if (missing) id = int.MaxValue;
        var result = action switch
        {
            "get" => await page.OnGetAsync(id),
            "close" => await page.OnPostSetOpenAsync(id, false),
            _ => await page.OnPostReallocateAsync(id)
        };
        if (missing) Assert.IsType<NotFoundResult>(result);
        else Assert.IsType<ForbidResult>(result);
        Assert.Equal(ExamStatuses.Open, Assert.Single(db.Sessions).Status);
        Assert.Empty(hub.Client.Messages);
    }

    [Fact]
    public async Task MissingStatus_DoesNotSilentlyCloseSession()
    {
        var page = Page(out var id);
        Assert.IsType<PageResult>(await page.OnPostSetOpenAsync(id, null));
        Assert.False(page.ModelState.IsValid);
        Assert.Equal(ExamStatuses.Open, Assert.Single(db.Sessions).Status);
        Assert.Empty(hub.Client.Messages);
    }

    [Fact]
    public async Task Reallocate_ProducesValidSetsBeforeNotifyingOnlyDetail()
    {
        var page = Page(out var id);
        hub.Client.OnSend = () =>
        {
            var people = db.Sessions.Single().ExamParticipants.ToArray();
            Assert.All(people, p => Assert.Equal(2, p.ParticipantQuestions.Count));
            Assert.Empty(people[0].ParticipantQuestions.Select(q => q.QuestionId)
                .Intersect(people[1].ParticipantQuestions.Select(q => q.QuestionId)));
        };
        Assert.IsType<RedirectToPageResult>(await page.OnPostReallocateAsync(id));
        Assert.Equal($"exam-{id}", Assert.Single(hub.Client.Messages).Args[0]);
    }

    [Theory]
    [InlineData(ParticipantStatuses.InProgress)]
    [InlineData(ParticipantStatuses.Completed)]
    public async Task Reallocate_StalePageCannotChangeQuestionsAfterSomeoneStarts(string status)
    {
        var page = Page(out var id);
        await page.OnGetAsync(id);
        Assert.True(page.CanReallocate);
        var person = db.Sessions.Single().ExamParticipants.First();
        var original = person.ParticipantQuestions.Select(q => q.QuestionId).ToArray();
        person.Status = status;
        Assert.IsType<PageResult>(await page.OnPostReallocateAsync(id));
        Assert.False(page.CanReallocate);
        Assert.False(page.ModelState.IsValid);
        Assert.Equal(original, person.ParticipantQuestions.Select(q => q.QuestionId));
        Assert.Empty(hub.Client.Messages);
    }

    [Fact]
    public async Task Reallocate_InsufficientActiveBank_PreservesQuestionsAndDisplaysError()
    {
        var page = Page(out var id);
        foreach (var question in db.Questions) question.IsActive = false;
        Assert.IsType<PageResult>(await page.OnPostReallocateAsync(id));
        Assert.False(page.ModelState.IsValid);
        Assert.All(page.Exam.Participants, p => Assert.Equal(2, p.Questions.Count));
        Assert.Empty(hub.Client.Messages);
    }
}
