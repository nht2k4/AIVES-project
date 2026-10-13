using System.ComponentModel.DataAnnotations;
using AIVES.Ass2.Razor.Infrastructure;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVES.Ass2.Razor.Pages.Accounts;

// TV4 - Ass2 - việc 4.2: tạo tài khoản. Không cho chọn vai trò Pending.
[Authorize(Roles = Roles.Admin)]
public class CreateModel(IAccountService accounts) : AppPageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Nhập họ tên.")]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nhập mật khẩu.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu từ 6 đến 100 ký tự.")]
        [RegularExpression(@"^\S+$", ErrorMessage = "Mật khẩu không được chứa khoảng trắng.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Chọn vai trò.")]
        [Display(Name = "Vai trò")]
        public AppRole? Role { get; set; }
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.Role is null or AppRole.Pending)
            ModelState.AddModelError("Input.Role", "Chọn vai trò Admin hoặc Giảng viên.");
        if (!ModelState.IsValid)
            return Page();

        var result = await accounts.CreateAsync(new CreateAccountRequest(
            Input.FullName, Input.Email, Input.Password, Input.Role!.Value));
        if (!result.Succeeded)
        {
            var mapped = MapFailure(result);
            if (mapped is not null) return mapped;
            ModelState.AddModelError(string.Empty, result.Error!);
            return Page();
        }

        FlashSuccess($"Đã tạo tài khoản {Input.Email.Trim().ToLowerInvariant()}.");
        return RedirectToPage("./Index");
    }
}
