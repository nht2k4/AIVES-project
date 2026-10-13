using AIVES.Business.Models;

namespace AIVES.Ass2.Razor.Pages;

public static class TV4Ui
{
    public static string RoleName(AppRole role) => role switch
    {
        AppRole.Admin => "Quản trị viên",
        AppRole.Lecturer => "Giảng viên",
        AppRole.Pending => "Chờ cấp quyền",
        _ => role.ToString()
    };
}
