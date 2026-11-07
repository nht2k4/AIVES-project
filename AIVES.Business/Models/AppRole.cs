using System.ComponentModel.DataAnnotations;

namespace AIVES.Business.Models;

// Enum riêng của tầng Business: tầng Presentation dùng enum này, không chạm vào Entity của DataAccess
public enum AppRole
{
    [Display(Name = "Quản trị viên")] Admin = 1,
    [Display(Name = "Giảng viên")] Lecturer = 2,
    [Display(Name = "Sinh viên")] Student = 3,

    // Tự đăng ký: chưa có quyền gì, chưa đăng nhập được cho tới khi quản trị viên cấp vai trò
    [Display(Name = "Chờ cấp quyền")] Pending = 4
}

public static class Roles
{
    public const string Admin = nameof(AppRole.Admin);
    public const string Lecturer = nameof(AppRole.Lecturer);
    public const string Student = nameof(AppRole.Student);
    public const string Pending = nameof(AppRole.Pending);
}
