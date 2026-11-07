using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.DataAccess.Repositories;

// TV2 - Ass3 - việc 2.2. Xem AIVES.Tests.Data/RepositoryTests.cs (VoiceRepositoryTests).
public class VoiceRepository : IVoiceRepository
{
    private readonly AppDbContext _context;

    public VoiceRepository(AppDbContext context) => _context = context;

    public Task<List<Voice>> GetAllAsync() => throw new NotImplementedException();

    public Task<Voice?> GetByNameAsync(string name) => throw new NotImplementedException();

    public Task AddAsync(Voice voice) => throw new NotImplementedException();

    public Task<bool> RemoveAsync(string name) => throw new NotImplementedException();
}
