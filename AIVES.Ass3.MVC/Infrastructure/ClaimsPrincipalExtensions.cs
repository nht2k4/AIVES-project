using System.Security.Claims;

namespace AIVES.Ass3.MVC.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    public static int? GetUserId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    public static int RequireUserId(this ClaimsPrincipal user) =>
        user.GetUserId() ?? throw new InvalidOperationException("Người dùng chưa đăng nhập.");

    public static string? GetUserRole(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.Role)?.Value;
}
