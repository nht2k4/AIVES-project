using AIVES.Business.Common;
using AIVES.Business.DTOs;

namespace AIVES.Business.Interfaces;

public interface ISubjectService
{
    Task<List<SubjectDto>> GetAllAsync();
    Task<List<SubjectDto>> GetByLecturerAsync(int lecturerId);
    Task<ServiceResult<SubjectDetailDto>> GetDetailAsync(int subjectId);
}
