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
        _context.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Email == email);

    public Task<bool> EmailExistsAsync(string email, int? excludeId = null)
    {
        var query = _context.Accounts.AsNoTracking().Where(a => a.Email == email);
        if (excludeId is int id)
            query = query.Where(a => a.Id != id);
        return query.AnyAsync();
    }

    public async Task<List<Account>> SearchAsync(string? keyword, string? role)
    {
        var query = _context.Accounts.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(a => a.FullName.Contains(keyword) || a.Email.Contains(keyword));
        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(a => a.Role == role);
        return await query.OrderBy(a => a.Role).ThenBy(a => a.FullName).ToListAsync();
    }

    public Task<List<Account>> GetActiveByRoleAsync(string role) =>
        _context.Accounts.AsNoTracking()
            .Where(a => a.IsActive && a.Role == role)
            .OrderBy(a => a.FullName)
            .ToListAsync();

    public Task<int> CountActiveByRoleAsync(string role) =>
        _context.Accounts.CountAsync(a => a.IsActive && a.Role == role);

    public async Task AddAsync(Account account)
    {
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
    }

    public Task UpdateAsync(Account account) => _context.SaveChangesAsync();
}
