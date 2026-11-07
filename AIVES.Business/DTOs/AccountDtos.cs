using AIVES.Business.Models;

namespace AIVES.Business.DTOs;

public record AccountDto(int Id, string FullName, string Email, AppRole Role, bool IsActive, DateTime CreatedAt, string? StudentCode = null);

public record CreateAccountRequest(string FullName, string Email, string Password, AppRole Role, string? StudentCode = null);

public record UpdateAccountRequest(int Id, string FullName, string Email, AppRole Role, string? StudentCode = null);

// Người dùng tự tạo tài khoản: không chọn vai trò, quản trị viên cấp sau
public record RegisterRequest(string FullName, string Email, string Password);

public record LoginResult(int Id, string FullName, string Email, AppRole Role);
