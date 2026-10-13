using System.ComponentModel.DataAnnotations;
using AIVES.Ass2.Razor.Infrastructure;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;

namespace AIVES.Ass2.Razor.Pages.Auth;

// TV4 - Ass2 - việc 4.1: tự đăng ký (vai trò Pending) và báo SignalR topic accounts.
[AllowAnonymous]
public class RegisterModel(IAccountService accounts, IHubContext<LiveHub> live) : PageModel
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

        [Required(ErrorMessage = "Nhập lại mật khẩu.")]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nhập lại mật khẩu")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect("~/");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect("~/");
        if (!ModelState.IsValid)
            return Page();

        var result = await accounts.RegisterAsync(new RegisterRequest(Input.FullName, Input.Email, Input.Password));
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return Page();
        }

        await live.NotifyAsync("Có tài khoản mới đăng ký, đang chờ cấp vai trò.", LiveTopics.Accounts);
        TempData["Success"] = "Đã tạo tài khoản. Bạn đăng nhập được sau khi quản trị viên cấp vai trò.";
        return RedirectToPage("./Login");
    }
}
