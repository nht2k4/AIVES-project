using System.Net.Mail;

namespace AIVES.Business.Services;

internal static class AccountRules
{
    public const int MinPasswordLength = 6;

    public static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    public static bool IsValidEmail(string email) =>
        email.Contains('@') && MailAddress.TryCreate(email, out _);

    public static bool IsValidPassword(string? password) =>
    !string.IsNullOrEmpty(password)
    && password.Length >= MinPasswordLength
    && !password.Any(char.IsWhiteSpace);
    public static bool HasWhitespace(string? password) =>
    password is not null && password.Any(char.IsWhiteSpace);
}