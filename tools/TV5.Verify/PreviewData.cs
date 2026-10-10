using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using AIVES.DataAccess.Entities;
using AIVES.Tests.Support;

namespace TV5.Verify;

// Test substitutes only. Production TV2/TV3 files are not changed by this host.
public sealed class PreviewData
{
    public TestDb Db { get; } = new();
    public int AdminId { get; }
    public int AnId { get; }
    public int BinhId { get; }
    public int PendingId { get; }
    public int LockedId { get; }
    public int PrnId { get; }
    public int SwpId { get; }
    public int StoppedId { get; }
    public PreviewData()
    {
        AdminId = Db.AddAccount(Roles.Admin, "admin@fu.edu.vn", name: "Quản trị hệ thống").Id;
        AnId = Db.AddAccount(Roles.Lecturer, "an.nv@fu.edu.vn", name: "Nguyễn Văn An").Id;
        BinhId = Db.AddAccount(Roles.Lecturer, "binh.tt@fu.edu.vn", name: "Trần Thanh Bình").Id;
        PendingId = Db.AddAccount(Roles.Pending, "pending@fu.edu.vn").Id;
        LockedId = Db.AddAccount(Roles.Lecturer, "locked@fu.edu.vn", active: false).Id;
        var prn = Db.AddSubject("PRN222");
        PrnId = prn.Id;
        prn.Name = "Lập trình ứng dụng với Razor Pages";
        var enw = Db.AddSubject("ENW492c");
        enw.Name = "English Writing";
        var swp = Db.AddSubject("SWP391");
        SwpId = swp.Id;
        swp.Name = "Software Development Project";
        StoppedId = Db.AddSubject("PRN211", active: false).Id;
        Db.AddSubject("PRU213");
        Db.Assign(Db.Accounts.Single(a => a.Id == AnId), prn);
        Db.Assign(Db.Accounts.Single(a => a.Id == BinhId), enw);
        Db.AddQuestion(prn, "Dependency Injection là gì?", "Giảm phụ thuộc;Dễ kiểm thử");
        Db.AddQuestion(prn, "Razor Pages xử lý form như thế nào?", active: false);
        Db.AddQuestion(swp, "Câu hỏi môn khác");
    }
}

internal sealed class PreviewAssignments(PreviewData data) : IAssignmentService
{
    public Task<bool> CanAccessSubjectAsync(int accountId, int subjectId) => new PermissionDouble(data.Db).CanAccessSubjectAsync(accountId, subjectId);
    public Task<ServiceResult> AssignAsync(int lecturerId, int subjectId)
    {
        var a = data.Db.Accounts.SingleOrDefault(x => x.Id == lecturerId);
        var s = data.Db.Subjects.SingleOrDefault(x => x.Id == subjectId);
        if (a is null) return Task.FromResult(ServiceResult.NotFound("Không tìm thấy giảng viên."));
        if (a.Role != Roles.Lecturer || !a.IsActive) return Task.FromResult(ServiceResult.Fail("Giảng viên không hợp lệ."));
        if (s is null) return Task.FromResult(ServiceResult.NotFound("Không tìm thấy môn học."));
        if (!s.IsActive) return Task.FromResult(ServiceResult.Fail("Môn học đã ngừng hoạt động."));
        if (data.Db.LecturerSubjects.Any(x => x.LecturerId == lecturerId && x.SubjectId == subjectId))
            return Task.FromResult(ServiceResult.Fail("Giảng viên đã được phân công."));
        data.Db.Assign(a, s);
        return Task.FromResult(ServiceResult.Ok());
    }
    public async Task<ServiceResult> UnassignAsync(int lecturerId, int subjectId) =>
        await data.Db.LecturerSubjectRepo.RemoveAsync(lecturerId, subjectId) ? ServiceResult.Ok() : ServiceResult.NotFound("Không tìm thấy phân công.");
}

internal sealed class PreviewSubjects(PreviewData data) : ISubjectService
{
    private static SubjectDto Map(Subject s) => new(s.Id, s.Code, s.Name, s.IsActive,
        AppLanguageExtensions.FromLocaleCode(s.SttLanguage), AppLanguageExtensions.FromLocaleCode(s.TtsLanguage), s.LecturerSubjects.Count);
    public Task<List<SubjectDto>> GetAllAsync() => Task.FromResult(data.Db.Subjects.OrderBy(s => s.Code).Select(Map).ToList());
    public Task<List<SubjectDto>> GetByLecturerAsync(int id) => Task.FromResult(data.Db.Subjects
        .Where(s => s.LecturerSubjects.Any(a => a.LecturerId == id)).OrderBy(s => s.Code).Select(Map).ToList());
    public Task<ServiceResult<SubjectDetailDto>> GetDetailAsync(int subjectId)
    {
        var s = data.Db.Subjects.SingleOrDefault(x => x.Id == subjectId);
        if (s is null) return Task.FromResult(ServiceResult<SubjectDetailDto>.NotFound("Không tìm thấy môn học."));
        return Task.FromResult(ServiceResult<SubjectDetailDto>.Ok(new(Map(s),
            s.LecturerSubjects.Select(a => new AssignedLecturerDto(a.LecturerId, a.Lecturer.FullName, a.Lecturer.Email, a.Lecturer.IsActive, a.AssignedAt)).ToList(),
            data.Db.Accounts.Where(a => a.IsActive && a.Role == Roles.Lecturer && !s.LecturerSubjects.Any(l => l.LecturerId == a.Id))
                .Select(a => new LecturerOptionDto(a.Id, a.FullName, a.Email)).ToList())));
    }
}

internal sealed class PreviewLanguages(PreviewData data, IAssignmentService permissions) : ILanguageConfigService
{
    private readonly Dictionary<int, (DateTime time, string by)> _updates = new();
    public async Task<ServiceResult<LanguageConfigDto>> GetForEditAsync(int userId, int subjectId)
    {
        var s = data.Db.Subjects.SingleOrDefault(s => s.Id == subjectId);
        if (s is null) return ServiceResult<LanguageConfigDto>.NotFound("Không tìm thấy môn học.");
        if (!await permissions.CanAccessSubjectAsync(userId, subjectId)) return ServiceResult<LanguageConfigDto>.Forbidden("Không có quyền.");
        var updated = _updates.GetValueOrDefault(subjectId);
        return ServiceResult<LanguageConfigDto>.Ok(new(s.Id, s.Code, s.Name, AppLanguageExtensions.FromLocaleCode(s.SttLanguage),
            AppLanguageExtensions.FromLocaleCode(s.TtsLanguage), updated.time == default ? null : updated.time, updated.by));
    }
    public async Task<ServiceResult> UpdateAsync(int userId, UpdateLanguageRequest request)
    {
        var access = await GetForEditAsync(userId, request.SubjectId);
        if (!access.Succeeded) return access;
        if (!Enum.IsDefined(request.SttLanguage) || !Enum.IsDefined(request.TtsLanguage)) return ServiceResult.Fail("Ngôn ngữ không hợp lệ.");
        var s = data.Db.Subjects.Single(s => s.Id == request.SubjectId);
        if (!s.IsActive) return ServiceResult.Fail("Môn học đã ngừng hoạt động.");
        s.SttLanguage = request.SttLanguage.ToLocaleCode();
        s.TtsLanguage = request.TtsLanguage.ToLocaleCode();
        _updates[s.Id] = (DateTime.UtcNow, data.Db.Accounts.Single(a => a.Id == userId).FullName);
        return ServiceResult.Ok();
    }
    public Task<ServiceResult<SpeechConfig>> GetSpeechConfigAsync(int subjectId) => throw new NotSupportedException();
}
