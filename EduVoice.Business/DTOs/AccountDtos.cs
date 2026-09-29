using EduVoice.Business.Models;

namespace EduVoice.Business.DTOs;

public record AccountDto(int Id, string FullName, string Email, AppRole Role, bool IsActive, DateTime CreatedAt);

public record CreateAccountRequest(string FullName, string Email, string Password, AppRole Role);

public record UpdateAccountRequest(int Id, string FullName, string Email, AppRole Role);

public record LoginResult(int Id, string FullName, string Email, AppRole Role);
