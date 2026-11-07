using System.ComponentModel.DataAnnotations;
using AIVES.Business.DTOs;
using AIVES.Business.Models;

namespace AIVES.Ass3.MVC.ViewModels;

public class AccountListViewModel
{
    public string? Keyword { get; set; }
    public AppRole? Role { get; set; }
    public List<AccountDto> Accounts { get; set; } = new();
}

public class AccountCreateViewModel
{
    [Required(ErrorMessage = "Nhập họ tên.")]
    [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(150)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập mật khẩu.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu từ 6 đến 100 ký tự.")]
    [RegularExpression(@"^\S+$", ErrorMessage = "Mật khẩu không được chứa khoảng trắng.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập lại mật khẩu.")]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nhập lại mật khẩu")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Vai trò")]
    public AppRole Role { get; set; } = AppRole.Lecturer;

    [StringLength(30, ErrorMessage = "Mã sinh viên tối đa 30 ký tự.")]
    [Display(Name = "Mã sinh viên (MSSV)")]
    public string? StudentCode { get; set; }
}

public class AccountEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nhập họ tên.")]
    [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(150)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Vai trò")]
    public AppRole Role { get; set; }

    [StringLength(30, ErrorMessage = "Mã sinh viên tối đa 30 ký tự.")]
    [Display(Name = "Mã sinh viên (MSSV)")]
    public string? StudentCode { get; set; }

    public bool IsActive { get; set; }
}
