using AIVES.DataAccess.Entities;

namespace AIVES.DataAccess.Repositories.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(int id);
    Task<Account?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email, int? excludeId = null);
    Task<List<Account>> SearchAsync(string? keyword, string? role);
    Task<List<Account>> GetActiveByRoleAsync(string role);
    Task<int> CountActiveByRoleAsync(string role);
    Task AddAsync(Account account);
    Task UpdateAsync(Account account);
}
