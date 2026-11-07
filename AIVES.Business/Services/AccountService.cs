using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.Business.Services;

// TV3 - Ass3 - việc 3.1. Luật nghiệp vụ của quản lý tài khoản. Xem AIVES_PhanCong_Ass2_Ass3.md và AIVES.Tests/Business/AccountTests.cs.
public class AccountService : IAccountService
{
    // Chữ ký hàm khởi tạo đã được chốt: test và phần dựng DI của TV6 đều dựa vào nó. Không đổi thứ tự tham số.
    public AccountService(IAccountRepository accounts, ILecturerSubjectRepository assignments, IPasswordHasher hasher)
    {
    }

    public Task<List<AccountDto>> SearchAsync(string? keyword, AppRole? role) => throw new NotImplementedException();

    public Task<ServiceResult<AccountDto>> GetByIdAsync(int id) => throw new NotImplementedException();

    public Task<ServiceResult<int>> CreateAsync(CreateAccountRequest request) => throw new NotImplementedException();

    public Task<ServiceResult<int>> RegisterAsync(RegisterRequest request) => throw new NotImplementedException();

    public Task<ServiceResult> UpdateAsync(int currentUserId, UpdateAccountRequest request) => throw new NotImplementedException();

    public Task<ServiceResult> SetActiveAsync(int currentUserId, int accountId, bool isActive) => throw new NotImplementedException();

    public Task<ServiceResult> ResetPasswordAsync(int accountId, string newPassword) => throw new NotImplementedException();
}
