using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AIVES.DataAccess.Repositories;

// TV2 - Ass3 - việc 2.2. Xem AIVES.Tests.Data/RepositoryTests.cs (VoiceRepositoryTests).
public class VoiceRepository : IVoiceRepository
{
    private readonly AppDbContext _context;

    public VoiceRepository(AppDbContext context) => _context = context;

    public Task<List<Voice>> GetAllAsync() =>
        _context.Voices.AsNoTracking().OrderBy(v => v.Name).Select(v => new Voice
        {
            Id = v.Id,
            Name = v.Name,
            Description = v.Description,
            FileName = v.FileName,
            Denoise = v.Denoise,
            Clip = Array.Empty<byte>(),
            CreatedBy = v.CreatedBy,
            CreatedAt = v.CreatedAt
        }).ToListAsync();

    public Task<Voice?> GetByNameAsync(string name) =>
        _context.Voices.AsNoTracking().FirstOrDefaultAsync(v => v.Name == name);

    public async Task AddAsync(Voice voice)
    {
        _context.Voices.Add(voice);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> RemoveAsync(string name)
    {
        var voice = await _context.Voices.FirstOrDefaultAsync(v => v.Name == name);
        if (voice is null) return false;

        var subjects = await _context.Subjects.Where(s => s.TtsVoice == voice.Name).ToListAsync();
        foreach (var subject in subjects)
            subject.TtsVoice = null;

        _context.Voices.Remove(voice);
        await _context.SaveChangesAsync();
        return true;
    }
}
