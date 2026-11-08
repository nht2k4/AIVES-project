using System.Security.Claims;
using AIVES.Ass3.MVC.Infrastructure;
using AIVES.Ass3.MVC.ViewModels;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace AIVES.Ass3.MVC.Controllers;

public class AuthController : AppController
{
    private readonly IAuthService _authService;
    private readonly IAccountService _accountService;
    private readonly IHubContext<LiveHub> _live;

    public AuthController(IAuthService authService, IAccountService accountService, IHubContext<LiveHub> live)
    {
        _authService = authService;
        _accountService = accountService;
        _live = live;
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _authService.LoginAsync(model.Email, model.Password);
        if (!result.Succeeded || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        var user = result.Data;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = model.RememberMe });

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new RegisterViewModel());
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        if (!ModelState.IsValid) return View(model);

        var result = await _accountService.RegisterAsync(new RegisterRequest(model.FullName, model.Email, model.Password));
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        await _live.NotifyAsync("Có tài khoản mới đăng ký, đang chờ cấp vai trò.", LiveTopics.Accounts);
        FlashSuccess("Đã tạo tài khoản. Bạn đăng nhập được sau khi quản trị viên cấp vai trò.");
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
