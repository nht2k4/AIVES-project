using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.DataAccess.Repositories;

// TV2 - Ass2 - việc 2.1. Xem AIVES.Tests.Data/RepositoryTests.cs (SubjectRepositoryTests).
public class SubjectRepository : ISubjectRepository
{
    private readonly AppDbContext _context;

    public SubjectRepository(AppDbContext context) => _context = context;

    public Task<Subject?> GetByIdAsync(int id) => throw new NotImplementedException();

    public Task<Subject?> GetWithLecturersAsync(int id) => throw new NotImplementedException();

    public Task<List<Subject>> GetAllWithAssignmentsAsync() => throw new NotImplementedException();

    public Task<List<Subject>> GetByLecturerAsync(int lecturerId) => throw new NotImplementedException();

    public Task UpdateAsync(Subject subject) => throw new NotImplementedException();
}
