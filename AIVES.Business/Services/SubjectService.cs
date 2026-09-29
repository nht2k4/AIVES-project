using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.Business.Services;

public class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _subjects;
    private readonly IAccountRepository _accounts;

    public SubjectService(ISubjectRepository subjects, IAccountRepository accounts)
    {
        _subjects = subjects;
        _accounts = accounts;
    }

    public async Task<List<SubjectDto>> GetAllAsync() =>
        (await _subjects.GetAllWithAssignmentsAsync()).Select(s => s.ToDto()).ToList();

    public async Task<List<SubjectDto>> GetByLecturerAsync(int lecturerId) =>
        (await _subjects.GetByLecturerAsync(lecturerId)).Select(s => s.ToDto()).ToList();

    public async Task<ServiceResult<SubjectDetailDto>> GetDetailAsync(int subjectId)
    {
        var subject = await _subjects.GetWithLecturersAsync(subjectId);
        if (subject is null)
            return ServiceResult<SubjectDetailDto>.NotFound("Không tìm thấy môn học.");

        var assigned = subject.LecturerSubjects
            .OrderBy(ls => ls.Lecturer.FullName)
            .Select(ls => new AssignedLecturerDto(
                ls.LecturerId, ls.Lecturer.FullName, ls.Lecturer.Email, ls.Lecturer.IsActive, ls.AssignedAt))
            .ToList();

        var assignedIds = assigned.Select(a => a.Id).ToHashSet();

        var available = (await _accounts.GetActiveByRoleAsync(Roles.Lecturer))
            .Where(a => !assignedIds.Contains(a.Id))
            .Select(a => new LecturerOptionDto(a.Id, a.FullName, a.Email))
            .ToList();

        return ServiceResult<SubjectDetailDto>.Ok(new SubjectDetailDto(subject.ToDto(), assigned, available));
    }
}
