using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.DataAccess.Repositories;

// TV2 - Ass2 - việc 2.1. Xem AIVES_PhanCong_Ass2_Ass3.md và AIVES.Tests.Data/RepositoryTests.cs (AccountRepositoryTests).
public class AccountRepository : IAccountRepository
{
    private readonly AppDbContext _context;

    public AccountRepository(AppDbContext context) => _context = context;

    public Task<Account?> GetByIdAsync(int id) => throw new NotImplementedException();

    public Task<Account?> GetByEmailAsync(string email) => throw new NotImplementedException();

    public Task<bool> EmailExistsAsync(string email, int? excludeId = null) => throw new NotImplementedException();

    public Task<List<Account>> SearchAsync(string? keyword, string? role) => throw new NotImplementedException();

    public Task<List<Account>> GetActiveByRoleAsync(string role) => throw new NotImplementedException();

    public Task<int> CountActiveByRoleAsync(string role) => throw new NotImplementedException();

    public Task AddAsync(Account account) => throw new NotImplementedException();

    public Task UpdateAsync(Account account) => throw new NotImplementedException();
}
