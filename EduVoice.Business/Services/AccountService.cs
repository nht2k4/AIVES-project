using EduVoice.Business.Common;
using EduVoice.Business.DTOs;
using EduVoice.Business.Interfaces;
using EduVoice.Business.Models;
using EduVoice.DataAccess.Entities;
using EduVoice.DataAccess.Repositories.Interfaces;

namespace EduVoice.Business.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accounts;
    private readonly ILecturerSubjectRepository _assignments;
    private readonly IPasswordHasher _hasher;

    public AccountService(IAccountRepository accounts, ILecturerSubjectRepository assignments, IPasswordHasher hasher)
    {
        _accounts = accounts;
        _assignments = assignments;
        _hasher = hasher;
    }

    public async Task<List<AccountDto>> SearchAsync(string? keyword, AppRole? role)
    {
        var accounts = await _accounts.SearchAsync(keyword, role?.ToEntity());
        return accounts.Select(a => a.ToDto()).ToList();
    }

    public async Task<ServiceResult<AccountDto>> GetByIdAsync(int id)
    {
        var account = await _accounts.GetByIdAsync(id);
        return account is null
            ? ServiceResult<AccountDto>.NotFound("Không tìm thấy tài khoản.")
            : ServiceResult<AccountDto>.Ok(account.ToDto());
    }

    public async Task<ServiceResult<int>> CreateAsync(CreateAccountRequest request)
    {
        var fullName = request.FullName?.Trim() ?? string.Empty;
        var email = AccountRules.NormalizeEmail(request.Email);

        if (fullName.Length == 0)
            return ServiceResult<int>.Fail("Họ tên không được để trống.");
        if (!AccountRules.IsValidEmail(email))
            return ServiceResult<int>.Fail("Email không hợp lệ.");
        if (!AccountRules.IsValidPassword(request.Password))
            return ServiceResult<int>.Fail($"Mật khẩu phải có ít nhất {AccountRules.MinPasswordLength} ký tự.");
        if (!Enum.IsDefined(request.Role))
            return ServiceResult<int>.Fail("Vai trò không hợp lệ.");
        if (await _accounts.EmailExistsAsync(email))
            return ServiceResult<int>.Fail($"Email {email} đã được dùng cho tài khoản khác.");

        var account = new Account
        {
            FullName = fullName,
            Email = email,
            PasswordHash = _hasher.Hash(request.Password),
            Role = request.Role.ToEntity(),
            IsActive = true
        };

        await _accounts.AddAsync(account);
        return ServiceResult<int>.Ok(account.Id);
    }

    public async Task<ServiceResult> UpdateAsync(int currentUserId, UpdateAccountRequest request)
    {
        var account = await _accounts.GetByIdAsync(request.Id);
        if (account is null)
            return ServiceResult.NotFound("Không tìm thấy tài khoản.");

        var fullName = request.FullName?.Trim() ?? string.Empty;
        var email = AccountRules.NormalizeEmail(request.Email);

        if (fullName.Length == 0)
            return ServiceResult.Fail("Họ tên không được để trống.");
        if (!AccountRules.IsValidEmail(email))
            return ServiceResult.Fail("Email không hợp lệ.");
        if (!Enum.IsDefined(request.Role))
            return ServiceResult.Fail("Vai trò không hợp lệ.");
        if (await _accounts.EmailExistsAsync(email, account.Id))
            return ServiceResult.Fail($"Email {email} đã được dùng cho tài khoản khác.");

        var newRole = request.Role.ToEntity();
        if (newRole != account.Role)
        {
            if (account.Id == currentUserId)
                return ServiceResult.Fail("Bạn không thể tự đổi vai trò của chính mình.");

            if (account.Role == UserRole.Admin && account.IsActive && await _accounts.CountActiveAdminsAsync() <= 1)
                return ServiceResult.Fail("Hệ thống phải còn ít nhất một quản trị viên đang hoạt động.");

            if (account.Role == UserRole.Lecturer && await _assignments.CountByLecturerAsync(account.Id) > 0)
                return ServiceResult.Fail("Giảng viên này đang được phân công môn học. Gỡ phân công trước khi đổi vai trò.");
        }

        account.FullName = fullName;
        account.Email = email;
        account.Role = newRole;

        await _accounts.UpdateAsync(account);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SetActiveAsync(int currentUserId, int accountId, bool isActive)
    {
        var account = await _accounts.GetByIdAsync(accountId);
        if (account is null)
            return ServiceResult.NotFound("Không tìm thấy tài khoản.");

        if (account.IsActive == isActive)
            return ServiceResult.Ok();

        if (!isActive)
        {
            if (account.Id == currentUserId)
                return ServiceResult.Fail("Bạn không thể tự khóa tài khoản của chính mình.");

            if (account.Role == UserRole.Admin && await _accounts.CountActiveAdminsAsync() <= 1)
                return ServiceResult.Fail("Không thể khóa quản trị viên cuối cùng đang hoạt động.");
        }

        account.IsActive = isActive;
        await _accounts.UpdateAsync(account);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ResetPasswordAsync(int accountId, string newPassword)
    {
        var account = await _accounts.GetByIdAsync(accountId);
        if (account is null)
            return ServiceResult.NotFound("Không tìm thấy tài khoản.");

        if (!AccountRules.IsValidPassword(newPassword))
            return ServiceResult.Fail($"Mật khẩu mới phải có ít nhất {AccountRules.MinPasswordLength} ký tự.");

        account.PasswordHash = _hasher.Hash(newPassword);
        await _accounts.UpdateAsync(account);
        return ServiceResult.Ok();
    }
}
