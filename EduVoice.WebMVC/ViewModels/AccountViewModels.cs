using System.ComponentModel.DataAnnotations;
using EduVoice.Business.DTOs;
using EduVoice.Business.Models;

namespace EduVoice.WebMVC.ViewModels;

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

    public bool IsActive { get; set; }
}
