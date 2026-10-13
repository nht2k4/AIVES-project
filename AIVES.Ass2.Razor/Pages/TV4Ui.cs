using AIVES.Business.Models;
using AIVES.Ass2.Razor.Infrastructure;

namespace AIVES.Ass2.Razor.Pages;

public static class TV4Ui
{
    public static string RoleName(AppRole role) => role.GetDisplayName();
}
