using EduVoice.Business.DTOs;
using EduVoice.Business.Interfaces;
using EduVoice.Business.Models;
using EduVoice.WebMVC.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduVoice.WebMVC.Controllers;

[Authorize(Roles = Roles.Admin)]
public class AccountsController : AppController
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService) => _accountService = accountService;

    public async Task<IActionResult> Index(string? keyword, AppRole? role)
    {
        var accounts = await _accountService.SearchAsync(keyword, role);
        return View(new AccountListViewModel { Keyword = keyword, Role = role, Accounts = accounts });
    }

    [HttpGet]
    public IActionResult Create() => View(new AccountCreateViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccountCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _accountService.CreateAsync(
            new CreateAccountRequest(model.FullName, model.Email, model.Password, model.Role));

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        FlashSuccess($"Đã tạo tài khoản {model.Email.Trim().ToLowerInvariant()}.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _accountService.GetByIdAsync(id);
        if (!result.Succeeded) return MapFailure(result) ?? NotFound();

        var a = result.Data!;
        return View(new AccountEditViewModel
        {
            Id = a.Id,
            FullName = a.FullName,
            Email = a.Email,
            Role = a.Role,
            IsActive = a.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AccountEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _accountService.UpdateAsync(
            CurrentUserId, new UpdateAccountRequest(model.Id, model.FullName, model.Email, model.Role));

        if (!result.Succeeded)
        {
            var mapped = MapFailure(result);
            if (mapped is not null) return mapped;

            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        FlashSuccess("Đã lưu thay đổi tài khoản.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(int id, bool isActive)
    {
        var result = await _accountService.SetActiveAsync(CurrentUserId, id, isActive);

        if (result.Succeeded) FlashSuccess(isActive ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.");
        else FlashError(result.Error!);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int id, string newPassword)
    {
        var result = await _accountService.ResetPasswordAsync(id, newPassword);

        if (result.Succeeded) FlashSuccess("Đã đặt lại mật khẩu.");
        else FlashError(result.Error!);

        return RedirectToAction(nameof(Edit), new { id });
    }
}
