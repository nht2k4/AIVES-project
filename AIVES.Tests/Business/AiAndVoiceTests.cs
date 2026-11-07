using AIVES.Business.Common;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using AIVES.Business.Services;
using AIVES.DataAccess.Entities;
using AIVES.Tests.Support;
using Xunit;

namespace AIVES.Tests.Business;

// ====== Ass3: AI hỏi xoáy (luật dự phòng, không cần internet) và giọng đọc VieNeu ======

public class FollowUpGeneratorFallbackTests
{
    // Không có ApiKey => luôn dùng luật dự phòng, nên test không gọi mạng
    private readonly FollowUpGenerator _ai = new(new AiOptions { ApiKey = null });

    private static FollowUpContext Ctx(string answer, string? points = "AddScoped;AddSingleton", string locale = "vi-VN",
        params (string, string)[] earlier) =>
        new("Giải thích DI?", points, locale, earlier.Append(("Giải thích DI?", answer)).ToList());

    [Fact]
    public async Task ExternalAiNotAllowed_UsesTheRules_EvenWithAKey()
    {
        // Có khóa nhưng môn tắt AI bên ngoài: không gọi mạng (khóa giả nên nếu gọi sẽ chậm và lỗi), trả lời ngay bằng luật
        var withKey = new FollowUpGenerator(new AiOptions { ApiKey = "khoa-gia" });
        var context = new FollowUpContext("Giải thích DI?", "AddScoped", "vi-VN", new[] { ("Giải thích DI?", "Không biết") }, AllowExternalAi: false);

        var watch = System.Diagnostics.Stopwatch.StartNew();
        var decision = await withKey.DecideAsync(context);

        Assert.True(decision.Ask);
        Assert.True(watch.ElapsedMilliseconds < 500);
    }

    [Theory]
    [InlineData("Không biết")]
    [InlineData("")]
    [InlineData("Cái đó dùng constructor")]
    public async Task ShortAnswer_IsFollowedUp(string answer)
    {
        var decision = await _ai.DecideAsync(Ctx(answer));

        Assert.True(decision.Ask);
        Assert.False(string.IsNullOrWhiteSpace(decision.Question));
        Assert.False(string.IsNullOrWhiteSpace(decision.Reason));
    }

    [Fact]
    public async Task LongAnswerMissingAKeyPoint_AsksAboutThatPoint()
    {
        var decision = await _ai.DecideAsync(Ctx(
            "Dependency injection là cách ASP.NET Core cung cấp đối tượng cho lớp qua constructor, mình thường dùng AddScoped để đăng ký service theo từng request"));

        Assert.True(decision.Ask);
        Assert.Contains("AddSingleton", decision.Question);
    }

    [Fact]
    public async Task LongAnswerCoveringEveryPoint_NeedsNoFollowUp()
    {
        var decision = await _ai.DecideAsync(Ctx(
            "Dependency injection cung cấp đối tượng qua constructor, đăng ký bằng AddScoped cho mỗi request hoặc AddSingleton cho toàn ứng dụng"));

        Assert.False(decision.Ask);
    }

    [Fact]
    public async Task PointMatching_IgnoresCaseAndVietnameseAccents()
    {
        var decision = await _ai.DecideAsync(Ctx(
            "Em dung dependency injection de tiem doi tuong vao lop qua constructor, dang ky bang ADDSCOPED hoac addsingleton tuy truong hop su dung",
            points: "AddScoped;AddSingleton"));

        Assert.False(decision.Ask);
    }

    [Fact]
    public async Task APointAlreadyAskedAbout_IsNotAskedAgain()
    {
        var longAnswer = "Dependency injection là cách ASP.NET Core cung cấp đối tượng cho lớp qua constructor, mình thường dùng AddScoped để đăng ký service";
        var decision = await _ai.DecideAsync(Ctx(longAnswer, earlier: ("Bạn chưa nhắc đến \"AddSingleton\". Bạn làm rõ được không?", "Dạ không nhớ")));

        Assert.False(decision.Ask);
    }

    [Fact]
    public async Task NoKeyPointsDefined_LongAnswerIsAccepted()
    {
        var decision = await _ai.DecideAsync(Ctx("Dependency injection là cách ASP.NET Core cung cấp đối tượng cho lớp qua constructor thay vì tự new", points: null));
        Assert.False(decision.Ask);
    }

    [Fact]
    public async Task EnglishLocale_AsksInEnglish()
    {
        var decision = await _ai.DecideAsync(Ctx("Not sure", locale: "en-US"));

        Assert.True(decision.Ask);
        Assert.Matches("[Cc]ould|[Yy]ou", decision.Question!);
        Assert.DoesNotMatch("[áàảãạăâđêôơư]", decision.Question!);
    }
}

public class VoiceServiceTests
{
    private readonly TestDb _db = new();
    private readonly FakeTextToSpeech _tts = new();
    private readonly Account _admin;
    private readonly Account _lecturer;
    private static readonly byte[] Clip = { 1, 2, 3, 4, 5 };

    public VoiceServiceTests()
    {
        _admin = _db.AddAccount(Roles.Admin);
        _lecturer = _db.AddAccount(Roles.Lecturer);
    }

    private VoiceService Service() => new(_tts, _db.VoiceRepo, _db.AccountRepo, new TtsOptions { Voice = "Mai Anh" });

    // Tên giọng duy nhất cho mỗi test (VoiceService nhớ các giọng đã nạp trong một biến static)
    private static string UniqueName() => "Giọng " + Guid.NewGuid().ToString("N")[..8];

    // ---- Clone
    [Fact]
    public async Task Clone_Admin_SavesTheClip_AndEnrollsItInTheTtsServer()
    {
        var name = UniqueName();

        var result = await Service().CloneAsync(_admin.Id, name, "Giọng thầy", Clip, "mau.wav", true);

        Assert.True(result.Succeeded, result.Error);
        var saved = Assert.Single(_db.Voices);
        Assert.Equal((name, "Giọng thầy", _admin.Id), (saved.Name, saved.Description, saved.CreatedBy));
        Assert.Equal(Clip, saved.Clip);
        Assert.True(_tts.Enrolled.ContainsKey(name));
    }

    [Fact]
    public async Task Clone_OnlyAdmins()
    {
        var result = await Service().CloneAsync(_lecturer.Id, UniqueName(), null, Clip, "mau.wav", true);

        Assert.Equal(ServiceErrorType.Forbidden, result.ErrorType);
        Assert.Empty(_db.Voices);
        Assert.Equal(0, _tts.EnrollCalls);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!bắt đầu bằng ký hiệu")]
    [InlineData("_gạch dưới đầu")]
    [InlineData("tên có / dấu gạch chéo")]
    public async Task Clone_InvalidName_IsRejected(string name)
    {
        var result = await Service().CloneAsync(_admin.Id, name, null, Clip, "mau.wav", true);

        Assert.False(result.Succeeded);
        Assert.Empty(_db.Voices);
    }

    [Fact]
    public async Task Clone_NameLongerThan64_IsRejected_But64IsFine()
    {
        Assert.False((await Service().CloneAsync(_admin.Id, new string('a', 65), null, Clip, "mau.wav", true)).Succeeded);
        Assert.True((await Service().CloneAsync(_admin.Id, "z" + Guid.NewGuid().ToString("N")[..31] + new string('b', 32), null, Clip, "mau.wav", true)).Succeeded);
    }

    [Fact]
    public async Task Clone_AcceptsLettersDigitsSpacesDotsDashesAndUnderscores()
    {
        var name = "Giọng Thầy-An_2.0 " + Guid.NewGuid().ToString("N")[..6];
        Assert.True((await Service().CloneAsync(_admin.Id, name, null, Clip, "mau.wav", true)).Succeeded);
    }

    [Fact]
    public async Task Clone_InvalidDescription_IsRejected()
    {
        Assert.False((await Service().CloneAsync(_admin.Id, UniqueName(), new string('d', 65), Clip, "mau.wav", true)).Succeeded);
        Assert.False((await Service().CloneAsync(_admin.Id, UniqueName(), "<script>", Clip, "mau.wav", true)).Succeeded);
    }

    [Theory]
    [InlineData("mau.exe")]
    [InlineData("mau")]
    [InlineData("mau.txt")]
    public async Task Clone_UnsupportedFileType_IsRejected(string fileName)
    {
        Assert.False((await Service().CloneAsync(_admin.Id, UniqueName(), null, Clip, fileName, true)).Succeeded);
    }

    [Theory]
    [InlineData("a.wav")]
    [InlineData("a.MP3")]
    [InlineData("a.flac")]
    [InlineData("a.ogg")]
    [InlineData("a.m4a")]
    public async Task Clone_SupportedFileTypes_AreAccepted(string fileName)
    {
        Assert.True((await Service().CloneAsync(_admin.Id, UniqueName(), null, Clip, fileName, true)).Succeeded);
    }

    [Fact]
    public async Task Clone_EmptyOrOversizeClip_IsRejected()
    {
        Assert.False((await Service().CloneAsync(_admin.Id, UniqueName(), null, Array.Empty<byte>(), "a.wav", true)).Succeeded);
        Assert.False((await Service().CloneAsync(_admin.Id, UniqueName(), null, new byte[20 * 1024 * 1024 + 1], "a.wav", true)).Succeeded);
    }

    [Fact]
    public async Task Clone_DuplicateName_IsRejected()
    {
        var name = UniqueName();
        await Service().CloneAsync(_admin.Id, name, null, Clip, "a.wav", true);

        Assert.False((await Service().CloneAsync(_admin.Id, name, null, Clip, "b.wav", true)).Succeeded);
        Assert.Single(_db.Voices);
    }

    [Fact]
    public async Task Clone_WhenTtsServerIsDown_SavesNothing()
    {
        _tts.Online = false;

        var result = await Service().CloneAsync(_admin.Id, UniqueName(), null, Clip, "a.wav", true);

        Assert.False(result.Succeeded);
        Assert.Empty(_db.Voices);
    }

    [Fact]
    public async Task Clone_UsingABuiltInVoiceName_IsRejectedByTheServer_AndNotSaved()
    {
        var result = await Service().CloneAsync(_admin.Id, "Mai Anh", null, Clip, "a.wav", true);

        Assert.False(result.Succeeded);
        Assert.Empty(_db.Voices);
    }

    // ---- Xóa
    [Fact]
    public async Task Delete_Admin_RemovesTheVoice_AndSubjectsUsingItGoBackToDefault()
    {
        var name = UniqueName();
        await Service().CloneAsync(_admin.Id, name, null, Clip, "a.wav", true);
        var subject = _db.AddSubject("PRN222");
        subject.TtsVoice = name;

        var result = await Service().DeleteAsync(_admin.Id, name);

        Assert.True(result.Succeeded);
        Assert.Empty(_db.Voices);
        Assert.Null(subject.TtsVoice);
    }

    [Fact]
    public async Task Delete_NonAdminForbidden_UnknownNotFound()
    {
        var name = UniqueName();
        await Service().CloneAsync(_admin.Id, name, null, Clip, "a.wav", true);

        Assert.Equal(ServiceErrorType.Forbidden, (await Service().DeleteAsync(_lecturer.Id, name)).ErrorType);
        Assert.Single(_db.Voices);
        Assert.Equal(ServiceErrorType.NotFound, (await Service().DeleteAsync(_admin.Id, "Giọng không có")).ErrorType);
    }

    // ---- Đọc
    [Fact]
    public async Task OpenSpeech_WithoutAVoice_UsesTheDefaultVoice()
    {
        var stream = await Service().OpenSpeechAsync("Xin chào", null);

        Assert.NotNull(stream);
        Assert.Equal("Mai Anh", _tts.SpokenWith.Last());
    }

    [Fact]
    public async Task OpenSpeech_ClonedVoiceTheServerForgot_IsEnrolledAgainFromTheDatabaseFirst()
    {
        var name = UniqueName();
        _db.Voices.Add(new Voice { Id = 1, Name = name, FileName = "clip.wav", Clip = Clip, Denoise = true, CreatedBy = _admin.Id });

        var stream = await Service().OpenSpeechAsync("Xin chào", name);

        Assert.NotNull(stream);
        Assert.True(_tts.Enrolled.ContainsKey(name));
        Assert.Equal(Clip, _tts.Enrolled[name]);
    }

    [Fact]
    public async Task OpenSpeech_ServerRestartedAndLostTheVoice_ReEnrollsOnceAndRetries()
    {
        var name = UniqueName();
        await Service().CloneAsync(_admin.Id, name, null, Clip, "a.wav", true); // đã nạp, VoiceService nhớ là đã nạp
        _tts.Enrolled.Clear();                                                    // VieNeu khởi động lại và quên
        var enrollCallsBefore = _tts.EnrollCalls;

        var stream = await Service().OpenSpeechAsync("Xin chào", name);

        Assert.NotNull(stream);
        Assert.Equal(enrollCallsBefore + 1, _tts.EnrollCalls);
        Assert.True(_tts.Enrolled.ContainsKey(name));
    }

    [Fact]
    public async Task OpenSpeech_UnknownVoice_FallsBackToTheDefaultVoice_InsteadOfStayingSilent()
    {
        var stream = await Service().OpenSpeechAsync("Xin chào", "Giọng không tồn tại");

        Assert.NotNull(stream);
        Assert.Equal(new string?[] { "Giọng không tồn tại", "Mai Anh" }, _tts.SpokenWith);
    }

    [Fact]
    public async Task OpenSpeech_WhenTheServerIsDown_ReturnsNull()
    {
        _tts.Online = false;
        Assert.Null(await Service().OpenSpeechAsync("Xin chào", null));
    }

    // ---- Tổng quan
    [Fact]
    public async Task Overview_MergesBuiltInAndClonedVoices_AndFlagsCloned()
    {
        var name = UniqueName();
        await Service().CloneAsync(_admin.Id, name, "Giọng thầy", Clip, "a.wav", true);

        var overview = await Service().GetOverviewAsync();

        Assert.True(overview.Status.Online);
        Assert.Equal("Mai Anh", overview.DefaultVoice);
        Assert.Contains(overview.Voices, v => v.Name == "Mai Anh" && !v.Cloned);
        Assert.Single(overview.Voices, v => v.Name == name); // giọng clone không bị liệt kê hai lần
        Assert.True(overview.Voices.Single(v => v.Name == name).Cloned);
    }

    [Fact]
    public async Task Overview_WhenTheServerIsDown_StillListsClonedVoices()
    {
        var name = UniqueName();
        _db.Voices.Add(new Voice { Id = 1, Name = name, FileName = "clip.wav", Clip = Clip, CreatedBy = _admin.Id });
        _tts.Online = false;

        var overview = await Service().GetOverviewAsync();

        Assert.False(overview.Status.Online);
        Assert.Equal(name, Assert.Single(overview.Voices).Name);
    }
}
