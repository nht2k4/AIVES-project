using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AIVES.DataAccess.Repositories;

public class SubjectRepository : ISubjectRepository
{
    private readonly AppDbContext _context;

    public SubjectRepository(AppDbContext context) => _context = context;

    public Task<Subject?> GetByIdAsync(int id) =>
        _context.Subjects.FirstOrDefaultAsync(s => s.Id == id);

    public Task<Subject?> GetWithLecturersAsync(int id) =>
        _context.Subjects.AsNoTracking()
            .Include(s => s.LecturerSubjects).ThenInclude(ls => ls.Lecturer)
            .FirstOrDefaultAsync(s => s.Id == id);

    public Task<List<Subject>> GetAllWithAssignmentsAsync() =>
        _context.Subjects.AsNoTracking()
            .Include(s => s.LecturerSubjects)
            .OrderBy(s => s.Code)
            .ToListAsync();

    public Task<List<Subject>> GetByLecturerAsync(int lecturerId) =>
        _context.Subjects.AsNoTracking()
            .Include(s => s.LecturerSubjects)
            .Where(s => s.LecturerSubjects.Any(ls => ls.LecturerId == lecturerId))
            .OrderBy(s => s.Code)
            .ToListAsync();

    public async Task UpdateAsync(Subject subject)
    {
        _context.Subjects.Update(subject);
        await _context.SaveChangesAsync();
    }
}
