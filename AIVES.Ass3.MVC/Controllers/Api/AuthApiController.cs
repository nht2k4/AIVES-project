using System.Security.Claims;
using AIVES.Ass3.MVC.Infrastructure;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace AIVES.Ass3.MVC.Controllers.Api;

[Route("api/auth")]
public class AuthApiController : ApiControllerBase
{
    private readonly IAuthService _auth;
    private readonly IAccountService _accounts;
    private readonly IHubContext<LiveHub> _live;

    public AuthApiController(IAuthService auth, IAccountService accounts, IHubContext<LiveHub> live)
    {
        _auth = auth;
        _accounts = accounts;
        _live = live;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult> Login([FromBody] LoginRequest req)
    {
        var result = await _auth.LoginAsync(req.Email, req.Password);
        if (!result.Succeeded || result.Data is null)
        {
            return StatusCode(StatusCodes.Status401Unauthorized, new { error = result.Error });
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
            new AuthenticationProperties { IsPersistent = true });

        return Ok(user);
    }

    [HttpPost("logout")]
    public async Task<ActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<ActionResult> Me()
    {
        var res = await _accounts.GetByIdAsync(CurrentUserId);
        return Respond(res);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult> Register([FromBody] RegisterRequest req)
    {
        var result = await _accounts.RegisterAsync(req);
        if (!result.Succeeded)
        {
            return Respond(result);
        }

        await _live.NotifyAsync("Có tài khoản mới đăng ký, đang chờ cấp vai trò.", LiveTopics.Accounts);
        return StatusCode(StatusCodes.Status201Created, new { id = result.Data });
    }
}

public record LoginRequest(string Email, string Password);
