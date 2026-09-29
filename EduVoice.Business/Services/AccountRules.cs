using System.Net.Mail;

namespace EduVoice.Business.Services;

internal static class AccountRules
{
    public const int MinPasswordLength = 6;

    public static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    public static bool IsValidEmail(string email) =>
        email.Contains('@') && MailAddress.TryCreate(email, out _);

    public static bool IsValidPassword(string? password) =>
        !string.IsNullOrWhiteSpace(password) && password.Length >= MinPasswordLength;
}
