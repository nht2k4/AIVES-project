using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AIVES.DataAccess.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly AppDbContext _context;

    public AccountRepository(AppDbContext context) => _context = context;

    public Task<Account?> GetByIdAsync(int id) =>
        _context.Accounts.FirstOrDefaultAsync(a => a.Id == id);

    public Task<Account?> GetByEmailAsync(string email) =>
        _context.Accounts.FirstOrDefaultAsync(a => a.Email == email);

    public Task<bool> EmailExistsAsync(string email, int? excludeId = null) =>
        _context.Accounts.AnyAsync(a => a.Email == email && (excludeId == null || a.Id != excludeId));

    public async Task<List<Account>> SearchAsync(string? keyword, string? role)
    {
        var query = _context.Accounts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(a => a.FullName.Contains(k) || a.Email.Contains(k));
        }

        if (!string.IsNullOrEmpty(role))
            query = query.Where(a => a.Role == role);

        return await query.OrderBy(a => a.Role).ThenBy(a => a.FullName).ToListAsync();
    }

    public Task<List<Account>> GetActiveByRoleAsync(string role) =>
        _context.Accounts.AsNoTracking()
            .Where(a => a.Role == role && a.IsActive)
            .OrderBy(a => a.FullName)
            .ToListAsync();

    public Task<int> CountActiveByRoleAsync(string role) =>
        _context.Accounts.CountAsync(a => a.Role == role && a.IsActive);

    public async Task AddAsync(Account account)
    {
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Account account)
    {
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync();
    }
}
