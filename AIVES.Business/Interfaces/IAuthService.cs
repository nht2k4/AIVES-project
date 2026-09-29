using AIVES.Business.Common;
using AIVES.Business.DTOs;

namespace AIVES.Business.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<LoginResult>> LoginAsync(string email, string password);

    // Trả về vai trò hiện tại nếu tài khoản còn hoạt động, null nếu đã bị khóa/không tồn tại
    Task<string?> GetActiveRoleAsync(int accountId);
}
