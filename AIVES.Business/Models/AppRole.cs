using System.ComponentModel.DataAnnotations;

namespace AIVES.Business.Models;

// Enum riêng của tầng Business: tầng Presentation dùng enum này, không chạm vào Entity của DataAccess
public enum AppRole
{
    [Display(Name = "Quản trị viên")] Admin = 1,
    [Display(Name = "Giảng viên")] Lecturer = 2
}

public static class Roles
{
    public const string Admin = nameof(AppRole.Admin);
    public const string Lecturer = nameof(AppRole.Lecturer);
}
