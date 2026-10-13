using System.ComponentModel.DataAnnotations;
using AIVES.Ass2.Razor.Infrastructure;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVES.Ass2.Razor.Pages.Accounts;

// TV4 - Ass2 - việc 4.2: sửa / cấp vai trò, khóa-mở khóa, đặt lại mật khẩu.
[Authorize(Roles = Roles.Admin)]
public class EditModel(IAccountService accounts) : AppPageModel
{
    public AccountDto Account { get; private set; } = null!;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public ResetInputModel Reset { get; set; } = new();

    public class InputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nhập họ tên.")]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Chọn vai trò.")]
        [Display(Name = "Vai trò")]
        public AppRole Role { get; set; }
    }

    public class ResetInputModel
    {
        [Required(ErrorMessage = "Nhập mật khẩu.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu từ 6 đến 100 ký tự.")]
        [RegularExpression(@"^\S+$", ErrorMessage = "Mật khẩu không được chứa khoảng trắng.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var failure = await LoadAsync(id);
        if (failure is not null) return failure;
        Input = new InputModel
        {
            Id = Account.Id,
            FullName = Account.FullName,
            Email = Account.Email,
            Role = Account.Role
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var failure = await LoadAsync(id);
        if (failure is not null) return failure;
        ModelState.Remove("Reset.NewPassword");
        if (!ModelState.IsValid)
            return Page();

        var result = await accounts.UpdateAsync(CurrentUserId, new UpdateAccountRequest(
            id, Input.FullName, Input.Email, Input.Role));
        if (!result.Succeeded)
        {
            failure = MapFailure(result);
            if (failure is not null) return failure;
            ModelState.AddModelError(string.Empty, result.Error!);
            return Page();
        }

        FlashSuccess("Đã lưu thay đổi tài khoản.");
        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostSetActiveAsync(int id, bool isActive)
    {
        var failure = await LoadAsync(id);
        if (failure is not null) return failure;

        var result = await accounts.SetActiveAsync(CurrentUserId, id, isActive);
        if (!result.Succeeded)
        {
            failure = MapFailure(result);
            if (failure is not null) return failure;
            FlashError(result.Error!);
            return RedirectToPage(new { id });
        }

        FlashSuccess(isActive ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.");
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(int id)
    {
        var failure = await LoadAsync(id);
        if (failure is not null) return failure;
        ModelState.Remove("Input.FullName");
        ModelState.Remove("Input.Email");
        ModelState.Remove("Input.Role");
        if (!ModelState.IsValid)
        {
            Input.Id = Account.Id;
            Input.FullName = Account.FullName;
            Input.Email = Account.Email;
            Input.Role = Account.Role;
            return Page();
        }

        var result = await accounts.ResetPasswordAsync(id, Reset.NewPassword);
        if (!result.Succeeded)
        {
            failure = MapFailure(result);
            if (failure is not null) return failure;
            ModelState.AddModelError(string.Empty, result.Error!);
            Input.Id = Account.Id;
            Input.FullName = Account.FullName;
            Input.Email = Account.Email;
            Input.Role = Account.Role;
            return Page();
        }

        FlashSuccess("Đã đặt lại mật khẩu.");
        return RedirectToPage(new { id });
    }

    private async Task<IActionResult?> LoadAsync(int id)
    {
        var result = await accounts.GetByIdAsync(id);
        if (!result.Succeeded)
            return MapFailure(result) ?? NotFound();
        Account = result.Data!;
        return null;
    }
}
