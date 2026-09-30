using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AIVES.DataAccess.Repositories;

public class LecturerSubjectRepository : ILecturerSubjectRepository
{
    private readonly AppDbContext _context;

    public LecturerSubjectRepository(AppDbContext context) => _context = context;

    public Task<bool> ExistsAsync(int lecturerId, int subjectId) =>
        _context.LecturerSubjects.AnyAsync(x => x.LecturerId == lecturerId && x.SubjectId == subjectId);

    public Task<int> CountByLecturerAsync(int lecturerId) =>
        _context.LecturerSubjects.CountAsync(x => x.LecturerId == lecturerId);

    public async Task AddAsync(LecturerSubject entity)
    {
        _context.LecturerSubjects.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> RemoveAsync(int lecturerId, int subjectId)
    {
        var entity = await _context.LecturerSubjects.FindAsync(lecturerId, subjectId);
        if (entity is null) return false;

        _context.LecturerSubjects.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }
}
