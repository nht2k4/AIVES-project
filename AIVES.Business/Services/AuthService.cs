using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.Business.Services;

// TV3 - Ass2 - việc 3.1. Xem AIVES.Tests/Business/AccountTests.cs (AuthServiceTests).
public class AuthService : IAuthService
{
    public AuthService(IAccountRepository accounts, IPasswordHasher hasher)
    {
    }

    public Task<ServiceResult<LoginResult>> LoginAsync(string email, string password) => throw new NotImplementedException();

    public Task<string?> GetActiveRoleAsync(int accountId) => throw new NotImplementedException();
}
