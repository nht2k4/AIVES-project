using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.Business.Services;

// TV3 - Ass3 - việc 3.1. Xem AIVES.Tests/Business/AccountTests.cs (AuthServiceTests).
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

        // Chặn Pending SAU khi đã kiểm mật khẩu đúng
        var role = Mapper.ParseRole(account.Role);
        if (role == AppRole.Pending)
            return ServiceResult<LoginResult>.Fail(
                "Tài khoản đang chờ quản trị viên cấp vai trò. Bạn đăng nhập được sau khi được cấp quyền.");

        return ServiceResult<LoginResult>.Ok(
            new LoginResult(account.Id, account.FullName, account.Email, role));
    }

    public Task<string?> GetActiveRoleAsync(int accountId) => throw new NotImplementedException();
}