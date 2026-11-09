using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.Business.Services;

// TV3 - Ass3 - việc 3.1. Luật nghiệp vụ của quản lý tài khoản. Xem AIVES_PhanCong_Ass2_Ass3.md và AIVES.Tests/Business/AccountTests.cs.
public class AccountService : IAccountService
{
    private const int MaxStudentCodeLength = 30;

    private readonly IAccountRepository _accounts;
    private readonly ILecturerSubjectRepository _assignments;
    private readonly IPasswordHasher _hasher;

    // Chữ ký hàm khởi tạo đã được chốt: test và phần dựng DI của TV6 đều dựa vào nó. Không đổi thứ tự tham số.
    public AccountService(IAccountRepository accounts, ILecturerSubjectRepository assignments, IPasswordHasher hasher)
    {
        _accounts = accounts;
        _assignments = assignments;
        _hasher = hasher;
    }

    public async Task<List<AccountDto>> SearchAsync(string? keyword, AppRole? role)
    {
        var accounts = await _accounts.SearchAsync(keyword, role?.ToString());
        return accounts.Select(a => a.ToDto()).ToList();
    }

    public async Task<ServiceResult<AccountDto>> GetByIdAsync(int id)
    {
        var account = await _accounts.GetByIdAsync(id);
        return account is null
            ? ServiceResult<AccountDto>.NotFound("Không tìm thấy tài khoản.")
            : ServiceResult<AccountDto>.Ok(account.ToDto());
    }
    public Task<ServiceResult<int>> CreateAsync(CreateAccountRequest request) =>
     CreateCoreAsync(request.FullName, request.Email, request.Password, request.Role, request.StudentCode);

    // Tự đăng ký: cùng luật như tạo tài khoản, nhưng vai trò luôn là Pending
    public Task<ServiceResult<int>> RegisterAsync(RegisterRequest request) =>
        CreateCoreAsync(request.FullName, request.Email, request.Password, AppRole.Pending, null);

    // Sinh viên: bắt buộc có mã (trim, không rỗng, <= 30). Trả về lỗi (null nghĩa là hợp lệ).
    private static string? CheckStudentCode(string? raw, out string? code)
    {
        code = raw?.Trim();
        if (string.IsNullOrEmpty(code))
            return "Sinh viên phải có mã sinh viên.";
        if (code.Length > MaxStudentCodeLength)
            return $"Mã sinh viên tối đa {MaxStudentCodeLength} ký tự.";
        return null;
    }

    private async Task<ServiceResult<int>> CreateCoreAsync(
        string? fullNameRaw, string? emailRaw, string? password, AppRole role, string? studentCodeRaw)
    {
        var fullName = fullNameRaw?.Trim() ?? string.Empty;
        var email = AccountRules.NormalizeEmail(emailRaw);

        if (fullName.Length == 0)
            return ServiceResult<int>.Fail("Họ tên không được để trống.");
        if (!AccountRules.IsValidEmail(email))
            return ServiceResult<int>.Fail("Email không hợp lệ.");
        if (AccountRules.HasWhitespace(password))
            return ServiceResult<int>.Fail("Mật khẩu không được chứa khoảng trắng.");
        if (!AccountRules.IsValidPassword(password))
            return ServiceResult<int>.Fail($"Mật khẩu phải có ít nhất {AccountRules.MinPasswordLength} ký tự.");
        if (!Enum.IsDefined(role))
            return ServiceResult<int>.Fail("Vai trò không hợp lệ.");
        if (await _accounts.EmailExistsAsync(email))
            return ServiceResult<int>.Fail($"Email {email} đã được dùng cho tài khoản khác.");

        string? studentCode = null;
        if (role == AppRole.Student)
        {
            var error = CheckStudentCode(studentCodeRaw, out studentCode);
            if (error is not null)
                return ServiceResult<int>.Fail(error);
            if (await _accounts.StudentCodeExistsAsync(studentCode!))
                return ServiceResult<int>.Fail($"Mã sinh viên {studentCode} đã được dùng cho tài khoản khác.");
        }

        var account = new Account
        {
            FullName = fullName,
            Email = email,
            PasswordHash = _hasher.Hash(password!),
            Role = role.ToString(),
            StudentCode = studentCode,
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

        string? studentCode = null;
        if (request.Role == AppRole.Student)
        {
            var error = CheckStudentCode(request.StudentCode, out studentCode);
            if (error is not null)
                return ServiceResult.Fail(error);
            if (await _accounts.StudentCodeExistsAsync(studentCode!, account.Id))
                return ServiceResult.Fail($"Mã sinh viên {studentCode} đã được dùng cho tài khoản khác.");
        }

        var newRole = request.Role.ToString();
        if (newRole != account.Role)
        {
            if (account.Id == currentUserId)
                return ServiceResult.Fail("Bạn không thể tự đổi vai trò của chính mình.");

            if (account.Role == Roles.Admin && account.IsActive
                && await _accounts.CountActiveByRoleAsync(Roles.Admin) <= 1)
                return ServiceResult.Fail("Hệ thống phải còn ít nhất một quản trị viên đang hoạt động.");

            if (account.Role == Roles.Lecturer && await _assignments.CountByLecturerAsync(account.Id) > 0)
                return ServiceResult.Fail("Giảng viên này đang được phân công môn học. Gỡ phân công trước khi đổi vai trò.");
        }

        account.FullName = fullName;
        account.Email = email;
        account.Role = newRole;
        account.StudentCode = request.Role == AppRole.Student ? studentCode : null; // vai khác: bỏ mã

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

            if (account.Role == Roles.Admin && await _accounts.CountActiveByRoleAsync(Roles.Admin) <= 1)
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

        if (AccountRules.HasWhitespace(newPassword))
            return ServiceResult.Fail("Mật khẩu mới không được chứa khoảng trắng.");
        if (!AccountRules.IsValidPassword(newPassword))
            return ServiceResult.Fail($"Mật khẩu mới phải có ít nhất {AccountRules.MinPasswordLength} ký tự.");

        account.PasswordHash = _hasher.Hash(newPassword);
        await _accounts.UpdateAsync(account);
        return ServiceResult.Ok();
    }
}