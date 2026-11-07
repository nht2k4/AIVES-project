using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Models;
using AIVES.Business.Services;
using AIVES.Tests.Support;
using Xunit;

namespace AIVES.Tests.Business;

// ====== phân công giảng viên, quyền truy cập môn, danh sách môn, cấu hình ngôn ngữ STT/TTS ======

public class AssignmentServiceTests
{
    private readonly TestDb _db = new();
    private AssignmentService Service() => new(_db.AccountRepo, _db.SubjectRepo, _db.LecturerSubjectRepo);

    [Fact]
    public async Task Assign_ActiveLecturerToActiveSubject_Works()
    {
        var lecturer = _db.AddAccount(Roles.Lecturer);
        var subject = _db.AddSubject("PRN222");

        var result = await Service().AssignAsync(lecturer.Id, subject.Id);

        Assert.True(result.Succeeded);
        Assert.Single(_db.LecturerSubjects);
    }

    [Fact]
    public async Task Assign_UnknownLecturerOrSubject_IsNotFound()
    {
        var lecturer = _db.AddAccount(Roles.Lecturer);
        var subject = _db.AddSubject("PRN222");

        Assert.Equal(ServiceErrorType.NotFound, (await Service().AssignAsync(999, subject.Id)).ErrorType);
        Assert.Equal(ServiceErrorType.NotFound, (await Service().AssignAsync(lecturer.Id, 999)).ErrorType);
    }

    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.Student)]
    public async Task Assign_OnlyLecturersCanBeAssigned(string role)
    {
        var account = _db.AddAccount(role, studentCode: role == Roles.Student ? "SE1" : null);
        var subject = _db.AddSubject("PRN222");

        var result = await Service().AssignAsync(account.Id, subject.Id);

        Assert.False(result.Succeeded);
        Assert.Empty(_db.LecturerSubjects);
    }

    [Fact]
    public async Task Assign_LockedLecturer_IsRejected()
    {
        var lecturer = _db.AddAccount(Roles.Lecturer, active: false);
        var subject = _db.AddSubject("PRN222");
        Assert.False((await Service().AssignAsync(lecturer.Id, subject.Id)).Succeeded);
    }

    [Fact]
    public async Task Assign_InactiveSubject_IsRejected()
    {
        var lecturer = _db.AddAccount(Roles.Lecturer);
        var subject = _db.AddSubject("PRN211", active: false);
        Assert.False((await Service().AssignAsync(lecturer.Id, subject.Id)).Succeeded);
    }

    [Fact]
    public async Task Assign_SamePairTwice_IsRejected()
    {
        var lecturer = _db.AddAccount(Roles.Lecturer);
        var subject = _db.AddSubject("PRN222");
        await Service().AssignAsync(lecturer.Id, subject.Id);

        var second = await Service().AssignAsync(lecturer.Id, subject.Id);

        Assert.False(second.Succeeded);
        Assert.Single(_db.LecturerSubjects);
    }

    [Fact]
    public async Task Unassign_RemovesTheLink_AndMissingLinkIsNotFound()
    {
        var lecturer = _db.AddAccount(Roles.Lecturer);
        var subject = _db.AddSubject("PRN222");
        _db.Assign(lecturer, subject);

        Assert.True((await Service().UnassignAsync(lecturer.Id, subject.Id)).Succeeded);
        Assert.Empty(_db.LecturerSubjects);
        Assert.Equal(ServiceErrorType.NotFound, (await Service().UnassignAsync(lecturer.Id, subject.Id)).ErrorType);
    }

    [Fact]
    public async Task CanAccess_Admin_IsAlwaysAllowed()
    {
        var admin = _db.AddAccount(Roles.Admin);
        var subject = _db.AddSubject("PRN222");
        Assert.True(await Service().CanAccessSubjectAsync(admin.Id, subject.Id));
    }

    [Fact]
    public async Task CanAccess_Lecturer_OnlyOnAssignedSubjects()
    {
        var lecturer = _db.AddAccount(Roles.Lecturer);
        var mine = _db.AddSubject("PRN222");
        var other = _db.AddSubject("SWP391");
        _db.Assign(lecturer, mine);

        Assert.True(await Service().CanAccessSubjectAsync(lecturer.Id, mine.Id));
        Assert.False(await Service().CanAccessSubjectAsync(lecturer.Id, other.Id));
    }

    [Fact]
    public async Task CanAccess_LockedAccount_StudentAndUnknownUser_AreDenied()
    {
        var subject = _db.AddSubject("PRN222");
        var lockedAdmin = _db.AddAccount(Roles.Admin, active: false);
        var student = _db.AddAccount(Roles.Student, studentCode: "SE1");

        Assert.False(await Service().CanAccessSubjectAsync(lockedAdmin.Id, subject.Id));
        Assert.False(await Service().CanAccessSubjectAsync(student.Id, subject.Id));
        Assert.False(await Service().CanAccessSubjectAsync(9999, subject.Id));
    }
}

public class SubjectServiceTests
{
    private readonly TestDb _db = new();
    private SubjectService Service() => new(_db.SubjectRepo, _db.AccountRepo);

    [Fact]
    public async Task GetAll_MapsFieldsAndCountsLecturers()
    {
        var an = _db.AddAccount(Roles.Lecturer);
        var binh = _db.AddAccount(Roles.Lecturer);
        var prn = _db.AddSubject("PRN222", stt: "vi-VN", tts: "en-US");
        _db.AddSubject("ENW492c", active: false);
        _db.Assign(an, prn);
        _db.Assign(binh, prn);

        var list = await Service().GetAllAsync();

        Assert.Equal(new[] { "ENW492c", "PRN222" }, list.Select(s => s.Code));
        var dto = list.Single(s => s.Code == "PRN222");
        Assert.Equal(2, dto.LecturerCount);
        Assert.Equal(AppLanguage.Vietnamese, dto.SttLanguage);
        Assert.Equal(AppLanguage.English, dto.TtsLanguage);
        Assert.False(list.Single(s => s.Code == "ENW492c").IsActive);
    }

    [Fact]
    public async Task GetByLecturer_ReturnsOnlyTheirSubjects()
    {
        var an = _db.AddAccount(Roles.Lecturer);
        var binh = _db.AddAccount(Roles.Lecturer);
        var prn = _db.AddSubject("PRN222");
        var swp = _db.AddSubject("SWP391");
        _db.Assign(an, prn);
        _db.Assign(binh, swp);

        var mine = await Service().GetByLecturerAsync(an.Id);

        Assert.Equal("PRN222", Assert.Single(mine).Code);
    }

    [Fact]
    public async Task GetDetail_SplitsAssignedAndAvailableLecturers()
    {
        var subject = _db.AddSubject("PRN222");
        var assigned = _db.AddAccount(Roles.Lecturer, name: "An");
        var available = _db.AddAccount(Roles.Lecturer, name: "Bình");
        _db.AddAccount(Roles.Lecturer, name: "Chi (bị khóa)", active: false);
        _db.AddAccount(Roles.Admin, name: "Quản trị");
        _db.Assign(assigned, subject);

        var result = await Service().GetDetailAsync(subject.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(assigned.Id, Assert.Single(result.Data!.AssignedLecturers).Id);
        // Chỉ giảng viên đang hoạt động và chưa được phân công mới nằm trong danh sách chọn
        Assert.Equal(available.Id, Assert.Single(result.Data.AvailableLecturers).Id);
    }

    [Fact]
    public async Task GetDetail_UnknownSubject_IsNotFound()
    {
        Assert.Equal(ServiceErrorType.NotFound, (await Service().GetDetailAsync(999)).ErrorType);
    }
}

public class LanguageConfigServiceTests
{
    private readonly TestDb _db = new();

    private LanguageConfigService Service() =>
        new(_db.SubjectRepo, _db.AccountRepo, new AssignmentService(_db.AccountRepo, _db.SubjectRepo, _db.LecturerSubjectRepo));

    [Fact]
    public async Task GetForEdit_AssignedLecturerAndAdmin_CanSee_OthersAreForbidden()
    {
        var subject = _db.AddSubject("PRN222", stt: "vi-VN", tts: "en-US");
        var mine = _db.AddAccount(Roles.Lecturer);
        var stranger = _db.AddAccount(Roles.Lecturer);
        var admin = _db.AddAccount(Roles.Admin);
        _db.Assign(mine, subject);

        var ok = await Service().GetForEditAsync(mine.Id, subject.Id);
        Assert.True(ok.Succeeded);
        Assert.Equal(AppLanguage.Vietnamese, ok.Data!.SttLanguage);
        Assert.Equal(AppLanguage.English, ok.Data.TtsLanguage);

        Assert.True((await Service().GetForEditAsync(admin.Id, subject.Id)).Succeeded);
        Assert.Equal(ServiceErrorType.Forbidden, (await Service().GetForEditAsync(stranger.Id, subject.Id)).ErrorType);
    }

    [Fact]
    public async Task GetForEdit_UnknownSubject_IsNotFound()
    {
        var admin = _db.AddAccount(Roles.Admin);
        Assert.Equal(ServiceErrorType.NotFound, (await Service().GetForEditAsync(admin.Id, 999)).ErrorType);
    }

    [Fact]
    public async Task Update_SavesLocaleCodes_AndWhoChangedIt()
    {
        var subject = _db.AddSubject("PRN222");
        var lecturer = _db.AddAccount(Roles.Lecturer, "an@fu.edu.vn");
        _db.Assign(lecturer, subject);

        var result = await Service().UpdateAsync(lecturer.Id, new UpdateLanguageRequest(subject.Id, AppLanguage.English, AppLanguage.Vietnamese));

        Assert.True(result.Succeeded);
        Assert.Equal("en-US", subject.SttLanguage);
        Assert.Equal("vi-VN", subject.TtsLanguage);
        Assert.Equal("an@fu.edu.vn", subject.LanguageUpdatedBy);
        Assert.NotNull(subject.LanguageUpdatedAt);
    }

    [Fact]
    public async Task Update_LecturerWhoIsNotAssigned_IsForbidden_AndNothingChanges()
    {
        var subject = _db.AddSubject("PRN222");
        var stranger = _db.AddAccount(Roles.Lecturer);

        var result = await Service().UpdateAsync(stranger.Id, new UpdateLanguageRequest(subject.Id, AppLanguage.English, AppLanguage.English));

        Assert.Equal(ServiceErrorType.Forbidden, result.ErrorType);
        Assert.Equal("vi-VN", subject.SttLanguage);
    }

    [Fact]
    public async Task Update_InvalidLanguage_InactiveSubject_UnknownSubject_AreRejected()
    {
        var admin = _db.AddAccount(Roles.Admin);
        var active = _db.AddSubject("PRN222");
        var inactive = _db.AddSubject("PRN211", active: false);

        Assert.False((await Service().UpdateAsync(admin.Id, new UpdateLanguageRequest(active.Id, (AppLanguage)99, AppLanguage.English))).Succeeded);
        Assert.False((await Service().UpdateAsync(admin.Id, new UpdateLanguageRequest(inactive.Id, AppLanguage.English, AppLanguage.English))).Succeeded);
        Assert.Equal(ServiceErrorType.NotFound, (await Service().UpdateAsync(admin.Id, new UpdateLanguageRequest(999, AppLanguage.English, AppLanguage.English))).ErrorType);
    }

    [Fact]
    public async Task Update_TtsVoice_NullKeeps_EmptyResets_NameSets()
    {
        var admin = _db.AddAccount(Roles.Admin);
        var subject = _db.AddSubject("PRN222");
        subject.TtsVoice = "Mai Anh";

        await Service().UpdateAsync(admin.Id, new UpdateLanguageRequest(subject.Id, AppLanguage.Vietnamese, AppLanguage.Vietnamese));
        Assert.Equal("Mai Anh", subject.TtsVoice);           // null = giữ nguyên

        await Service().UpdateAsync(admin.Id, new UpdateLanguageRequest(subject.Id, AppLanguage.Vietnamese, AppLanguage.Vietnamese, "  Hải Đăng "));
        Assert.Equal("Hải Đăng", subject.TtsVoice);          // có tên = đổi giọng (đã Trim)

        await Service().UpdateAsync(admin.Id, new UpdateLanguageRequest(subject.Id, AppLanguage.Vietnamese, AppLanguage.Vietnamese, ""));
        Assert.Null(subject.TtsVoice);                       // rỗng = về giọng mặc định
    }

    // ---- Thông số STT/AI chỉnh tay theo môn
    [Fact]
    public async Task Update_ManualSpeechSettings_AreSaved_AndNullKeepsThem()
    {
        var admin = _db.AddAccount(Roles.Admin);
        var subject = _db.AddSubject("PRN222");

        var result = await Service().UpdateAsync(admin.Id, new UpdateLanguageRequest(subject.Id, AppLanguage.Vietnamese, AppLanguage.Vietnamese,
            SttTerms: " ra dơ pây => Razor Pages \n\nđi ai => DI ", AiTimeoutSeconds: 5, UseExternalAi: false));

        Assert.True(result.Succeeded);
        Assert.Equal(("ra dơ pây => Razor Pages \n\nđi ai => DI", 5, false), (subject.SttTerms, subject.AiTimeoutSeconds, subject.UseExternalAi));

        // Trang Giọng đọc chỉ đổi giọng (các trường khác null): không được làm mất thông số đã chỉnh
        await Service().UpdateAsync(admin.Id, new UpdateLanguageRequest(subject.Id, AppLanguage.Vietnamese, AppLanguage.Vietnamese, "Mai Anh"));
        Assert.Equal((5, false), (subject.AiTimeoutSeconds, subject.UseExternalAi));
        Assert.NotNull(subject.SttTerms);

        await Service().UpdateAsync(admin.Id, new UpdateLanguageRequest(subject.Id, AppLanguage.Vietnamese, AppLanguage.Vietnamese, SttTerms: "  "));
        Assert.Null(subject.SttTerms);                       // rỗng = xóa từ điển
    }

    [Theory]
    [InlineData("chỉ có một vế", null)]                     // thiếu "=>"
    [InlineData("ra dơ pây =>", null)]                       // thiếu vế đúng
    [InlineData(null, 1)]
    [InlineData(null, 31)]
    public async Task Update_BadSttTermsOrAiTimeout_IsRejected(string? terms, int? timeout)
    {
        var admin = _db.AddAccount(Roles.Admin);
        var subject = _db.AddSubject("PRN222");

        var result = await Service().UpdateAsync(admin.Id, new UpdateLanguageRequest(subject.Id, AppLanguage.Vietnamese, AppLanguage.Vietnamese,
            SttTerms: terms, AiTimeoutSeconds: timeout));

        Assert.False(result.Succeeded);
        Assert.Equal((8, null as string), (subject.AiTimeoutSeconds, subject.SttTerms));
    }

    [Fact]
    public async Task Update_TtsVoiceLongerThan64_IsRejected()
    {
        var admin = _db.AddAccount(Roles.Admin);
        var subject = _db.AddSubject("PRN222");

        var result = await Service().UpdateAsync(admin.Id, new UpdateLanguageRequest(subject.Id, AppLanguage.Vietnamese, AppLanguage.Vietnamese, new string('x', 65)));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task GetSpeechConfig_ReturnsLocalesAndVoice_ForAnyone()
    {
        var subject = _db.AddSubject("ENW492c", stt: "en-US", tts: "en-US");
        subject.TtsVoice = "Mai Anh";
        subject.SttTerms = "ây pi ai => API";

        var result = await Service().GetSpeechConfigAsync(subject.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(new SpeechConfig("ENW492c", "en-US", "en-US", "Mai Anh", "ây pi ai => API", 8, true), result.Data);
        Assert.Equal(ServiceErrorType.NotFound, (await Service().GetSpeechConfigAsync(999)).ErrorType);
    }
}
