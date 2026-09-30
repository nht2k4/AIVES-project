using AIVES.DataAccess.Entities;

namespace AIVES.DataAccess.Repositories.Interfaces;

public interface ISubjectRepository
{
    Task<Subject?> GetByIdAsync(int id);
    Task<Subject?> GetWithLecturersAsync(int id);
    Task<List<Subject>> GetAllWithAssignmentsAsync();
    Task<List<Subject>> GetByLecturerAsync(int lecturerId);
    Task UpdateAsync(Subject subject);
}
