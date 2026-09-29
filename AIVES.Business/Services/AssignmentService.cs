using AIVES.Business.Common;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.Business.Services;

public class AssignmentService : IAssignmentService
{
    private readonly IAccountRepository _accounts;
    private readonly ISubjectRepository _subjects;
    private readonly ILecturerSubjectRepository _assignments;

    public AssignmentService(IAccountRepository accounts, ISubjectRepository subjects, ILecturerSubjectRepository assignments)
    {
        _accounts = accounts;
        _subjects = subjects;
        _assignments = assignments;
    }

    public async Task<ServiceResult> AssignAsync(int lecturerId, int subjectId)
    {
        var lecturer = await _accounts.GetByIdAsync(lecturerId);
        if (lecturer is null)
            return ServiceResult.NotFound("Không tìm thấy giảng viên.");
        if (lecturer.Role != Roles.Lecturer)
            return ServiceResult.Fail("Chỉ có thể phân công tài khoản có vai trò Giảng viên.");
        if (!lecturer.IsActive)
            return ServiceResult.Fail($"Tài khoản {lecturer.Email} đang bị khóa, không thể phân công.");

        var subject = await _subjects.GetByIdAsync(subjectId);
        if (subject is null)
            return ServiceResult.NotFound("Không tìm thấy môn học.");
        if (!subject.IsActive)
            return ServiceResult.Fail($"Môn {subject.Code} đã ngừng hoạt động, không nhận phân công mới.");

        if (await _assignments.ExistsAsync(lecturerId, subjectId))
            return ServiceResult.Fail($"{lecturer.FullName} đã được phân công môn {subject.Code} trước đó.");

        await _assignments.AddAsync(new LecturerSubject { LecturerId = lecturerId, SubjectId = subjectId });
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UnassignAsync(int lecturerId, int subjectId)
    {
        var removed = await _assignments.RemoveAsync(lecturerId, subjectId);
        return removed
            ? ServiceResult.Ok()
            : ServiceResult.NotFound("Giảng viên này không còn được phân công môn học này.");
    }

    public async Task<bool> CanAccessSubjectAsync(int accountId, int subjectId)
    {
        var account = await _accounts.GetByIdAsync(accountId);
        if (account is null || !account.IsActive) return false;

        return account.Role switch
        {
            Roles.Admin => true,
            Roles.Lecturer => await _assignments.ExistsAsync(accountId, subjectId),
            _ => false
        };
    }
}
