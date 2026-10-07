using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.DataAccess.Repositories;

// TV2 - Ass3 - việc 2.1. Xem AIVES.Tests.Data/RepositoryTests.cs (LecturerSubjectRepositoryTests).
public class LecturerSubjectRepository : ILecturerSubjectRepository
{
    private readonly AppDbContext _context;

    public LecturerSubjectRepository(AppDbContext context) => _context = context;

    public Task<bool> ExistsAsync(int lecturerId, int subjectId) => throw new NotImplementedException();

    public Task<int> CountByLecturerAsync(int lecturerId) => throw new NotImplementedException();

    public Task AddAsync(LecturerSubject entity) => throw new NotImplementedException();

    public Task<bool> RemoveAsync(int lecturerId, int subjectId) => throw new NotImplementedException();
}
