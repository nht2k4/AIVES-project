using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Models;

namespace AIVES.Business.Interfaces;

public interface IAccountService
{
    Task<List<AccountDto>> SearchAsync(string? keyword, AppRole? role);
    Task<ServiceResult<AccountDto>> GetByIdAsync(int id);
    Task<ServiceResult<int>> CreateAsync(CreateAccountRequest request);
    Task<ServiceResult> UpdateAsync(int currentUserId, UpdateAccountRequest request);
    Task<ServiceResult> SetActiveAsync(int currentUserId, int accountId, bool isActive);
    Task<ServiceResult> ResetPasswordAsync(int accountId, string newPassword);
}
