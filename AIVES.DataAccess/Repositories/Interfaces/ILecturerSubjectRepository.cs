using AIVES.DataAccess.Entities;

namespace AIVES.DataAccess.Repositories.Interfaces;

public interface ILecturerSubjectRepository
{
    Task<bool> ExistsAsync(int lecturerId, int subjectId);
    Task<int> CountByLecturerAsync(int lecturerId);
    Task AddAsync(LecturerSubject entity);
    Task<bool> RemoveAsync(int lecturerId, int subjectId);
}
