using System.Security.Claims;

namespace AIVES.Ass2.Razor.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    public static int GetAccountId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
