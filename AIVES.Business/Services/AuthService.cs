using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.Business.Services;

public class AuthService : IAuthService
{
    private readonly IAccountRepository _accounts;
    private readonly IPasswordHasher _hasher;

    public AuthService(IAccountRepository accounts, IPasswordHasher hasher)
    {
        _accounts = accounts;
        _hasher = hasher;
    }

    public async Task<ServiceResult<LoginResult>> LoginAsync(string email, string password)
    {
        var account = await _accounts.GetByEmailAsync(AccountRules.NormalizeEmail(email));

        // Cùng một thông báo cho "sai email" và "sai mật khẩu" để không lộ email nào tồn tại
        if (account is null || string.IsNullOrEmpty(password) || !_hasher.Verify(password, account.PasswordHash))
            return ServiceResult<LoginResult>.Fail("Email hoặc mật khẩu không đúng.");

        if (!account.IsActive)
            return ServiceResult<LoginResult>.Fail("Tài khoản đã bị khóa. Liên hệ quản trị viên để mở lại.");

        return ServiceResult<LoginResult>.Ok(
            new LoginResult(account.Id, account.FullName, account.Email, Mapper.ParseRole(account.Role)));
    }

    public async Task<string?> GetActiveRoleAsync(int accountId)
    {
        var account = await _accounts.GetByIdAsync(accountId);
        return account is { IsActive: true } ? Mapper.ParseRole(account.Role).ToString() : null;
    }
}
