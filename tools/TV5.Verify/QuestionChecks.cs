using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using AIVES.Business.Services;
using AIVES.Tests.Support;
using Xunit;

namespace TV5.Verify;

// This double is deliberately NOT TV3's AssignmentService.
internal sealed class PermissionDouble(TestDb db) : IAssignmentService
{
    public Task<bool> CanAccessSubjectAsync(int accountId, int subjectId)
    {
        var a = db.Accounts.SingleOrDefault(x => x.Id == accountId);
        return Task.FromResult(a is { IsActive: true } &&
            (a.Role == Roles.Admin || a.Role == Roles.Lecturer &&
             db.LecturerSubjects.Any(x => x.LecturerId == accountId && x.SubjectId == subjectId)));
    }
    public Task<ServiceResult> AssignAsync(int lecturerId, int subjectId) => throw new NotSupportedException();
    public Task<ServiceResult> UnassignAsync(int lecturerId, int subjectId) => throw new NotSupportedException();
}

public class QuestionChecks
{
    private readonly TestDb _db = new();
    private QuestionService Service => new(_db.QuestionRepo, new PermissionDouble(_db));
    private (int user, int subject) Setup()
    {
        var user = _db.AddAccount(Roles.Lecturer);
        var subject = _db.AddSubject("PRN222");
        _db.Assign(user, subject);
        return (user.Id, subject.Id);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Add_TrimsContent_AndNormalizesBlankPoints(string? points)
    {
        var (u, s) = Setup();
        Assert.True((await Service.AddAsync(u, new(s, "  DI là gì?  ", points))).Succeeded);
        var q = Assert.Single(_db.Questions);
        Assert.Equal("DI là gì?", q.Content);
        Assert.Null(q.ExpectedPoints);
        Assert.True(q.IsActive);
        Assert.Equal(s, q.SubjectId);
    }

    [Fact]
    public async Task Add_KeepsSemicolonPoints_AndAccepts1000Characters()
    {
        var (u, s) = Setup();
        Assert.True((await Service.AddAsync(u, new(s, new string('a', 1000), " a;b "))).Succeeded);
        Assert.Equal("a;b", Assert.Single(_db.Questions).ExpectedPoints);
        Assert.True((await Service.AddAsync(u, new(s, "B", new string('b', 1000)))).Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Add_Blank_IsRejectedWithoutMutation(string content)
    {
        var (u, s) = Setup();
        Assert.Equal(ServiceErrorType.Validation, (await Service.AddAsync(u, new(s, content, null))).ErrorType);
        Assert.Empty(_db.Questions);
    }

    [Fact]
    public async Task Add_OverLength_IsRejectedWithoutMutation()
    {
        var (u, s) = Setup();
        Assert.False((await Service.AddAsync(u, new(s, new string('a', 1001), null))).Succeeded);
        Assert.False((await Service.AddAsync(u, new(s, "A", new string('b', 1001)))).Succeeded);
        Assert.Empty(_db.Questions);
    }

    [Theory]
    [InlineData(Roles.Pending, true)]
    [InlineData(Roles.Lecturer, false)]
    [InlineData(Roles.Admin, false)]
    [InlineData(Roles.Lecturer, true)]
    [InlineData("Student", true)]
    public async Task EveryOperation_RejectsUnprivilegedUsers(string role, bool active)
    {
        var (_, s) = Setup();
        var other = _db.AddAccount(role, active: active);
        var q = _db.AddQuestion(_db.Subjects.Single());
        Assert.Equal(ServiceErrorType.Forbidden, (await Service.GetBySubjectAsync(other.Id, s)).ErrorType);
        Assert.Equal(ServiceErrorType.Forbidden, (await Service.AddAsync(other.Id, new(s, "A", null))).ErrorType);
        Assert.Equal(ServiceErrorType.Forbidden, (await Service.SetActiveAsync(other.Id, q.Id, false)).ErrorType);
        Assert.True(q.IsActive);
        Assert.Single(_db.Questions);
    }

    [Fact]
    public async Task UnknownUser_IsForbidden_AndAdminCanUseOtherSubjects()
    {
        var (_, s) = Setup();
        Assert.Equal(ServiceErrorType.Forbidden, (await Service.GetBySubjectAsync(999, s)).ErrorType);
        var admin = _db.AddAccount(Roles.Admin);
        Assert.True((await Service.AddAsync(admin.Id, new(s, "A", null))).Succeeded);
    }

    [Fact]
    public async Task Read_IncludesInactive_AndOnlyRequestedSubject()
    {
        var (u, s) = Setup();
        _db.AddQuestion(_db.Subjects.Single(), "A");
        _db.AddQuestion(_db.Subjects.Single(), "B", active: false);
        _db.AddQuestion(_db.AddSubject("SWP391"), "Other");
        var rows = (await Service.GetBySubjectAsync(u, s)).Data!;
        Assert.Equal(new[] { "A", "B" }, rows.Select(q => q.Content));
        Assert.Contains(rows, q => !q.IsActive);
    }

    [Fact]
    public async Task Toggle_UsesQuestionSubject_AndHandlesMissingQuestion()
    {
        var (u, _) = Setup();
        var mine = _db.AddQuestion(_db.Subjects.Single());
        var other = _db.AddQuestion(_db.AddSubject("Other"));
        Assert.True((await Service.SetActiveAsync(u, mine.Id, false)).Succeeded);
        Assert.False(mine.IsActive);
        Assert.True((await Service.SetActiveAsync(u, mine.Id, true)).Succeeded);
        Assert.True(mine.IsActive);
        Assert.Equal(ServiceErrorType.Forbidden, (await Service.SetActiveAsync(u, other.Id, false)).ErrorType);
        Assert.True(other.IsActive);
        Assert.Equal(ServiceErrorType.NotFound, (await Service.SetActiveAsync(u, 999, false)).ErrorType);
    }

    [Fact]
    public void Allocator_Reproducible_AndDoesNotMutatePool()
    {
        var pool = Enumerable.Range(1, 8).ToArray();
        var a = QuestionAllocator.Allocate(pool, 4, 3, new Random(12));
        var b = QuestionAllocator.Allocate(pool, 4, 3, new Random(12));
        Assert.Equal(a.SelectMany(x => x), b.SelectMany(x => x));
        Assert.Equal(Enumerable.Range(1, 8), pool);
        Assert.NotEqual(a.SelectMany(x => x), QuestionAllocator.Allocate(pool, 4, 3, new Random(24)).SelectMany(x => x));
    }

    [Fact]
    public void Allocator_InvalidInput_IsExplicit_AndDuplicatesNeverProduceRepeatedIds()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => QuestionAllocator.Allocate(new[] { 1 }, -1, 1, new Random()));
        Assert.Throws<ArgumentOutOfRangeException>(() => QuestionAllocator.Allocate(new[] { 1 }, 1, 0, new Random()));
        Assert.Throws<ArgumentException>(() => QuestionAllocator.Allocate(new[] { 1, 1 }, 1, 2, new Random()));
        Assert.Throws<ArgumentNullException>(() => QuestionAllocator.Allocate(null!, 1, 1, new Random()));
        Assert.Throws<ArgumentNullException>(() => QuestionAllocator.Allocate(new[] { 1 }, 1, 1, null!));
        var sets = QuestionAllocator.Allocate(new[] { 1, 1, 2, 3, 4 }, 10, 2, new Random(3));
        Assert.All(sets, s => Assert.Equal(2, s.Distinct().Count()));
        for (var i = 1; i < sets.Count; i++) Assert.Empty(sets[i].Intersect(sets[i - 1]));
    }

    [Fact]
    public void Allocator_ManySizesAndSeeds_AlwaysRespectCapacityAndNeighbourRule()
    {
        for (var seed = 0; seed < 30; seed++)
        for (var size = 1; size <= 16; size++)
        for (var per = 1; per <= size; per++)
        {
            var pool = Enumerable.Range(1, size).ToArray();
            var sets = QuestionAllocator.Allocate(pool, 20, per, new Random(seed));
            Assert.Equal(20, sets.Count);
            Assert.All(sets, s => { Assert.Equal(per, s.Length); Assert.Equal(per, s.Distinct().Count()); Assert.All(s, q => Assert.Contains(q, pool)); });
            if (size >= per * 2)
                for (var i = 1; i < sets.Count; i++) Assert.Empty(sets[i].Intersect(sets[i - 1]));
        }
    }
}
