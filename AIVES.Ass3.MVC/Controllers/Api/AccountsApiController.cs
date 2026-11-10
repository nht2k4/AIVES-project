using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVES.Ass3.MVC.Controllers.Api;

[Route("api/accounts")]
[Authorize(Roles = "Admin")]
public class AccountsApiController : ApiControllerBase
{
    private readonly IAccountService _accounts;

    public AccountsApiController(IAccountService accounts)
    {
        _accounts = accounts;
    }

    [HttpGet]
    public async Task<ActionResult<List<AccountDto>>> GetAll([FromQuery] string? keyword, [FromQuery] AppRole? role)
    {
        var list = await _accounts.SearchAsync(keyword, role);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var result = await _accounts.GetByIdAsync(id);
        return Respond(result);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateAccountRequest req)
    {
        var result = await _accounts.CreateAsync(req);
        if (!result.Succeeded)
        {
            return Respond(result);
        }

        return StatusCode(StatusCodes.Status201Created, new { id = result.Data });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] UpdateAccountBody req)
    {
        var request = new UpdateAccountRequest(id, req.FullName, req.Email, req.Role, req.StudentCode);
        var result = await _accounts.UpdateAsync(CurrentUserId, request);
        return Respond(result);
    }

    [HttpPatch("{id:int}/active")]
    public async Task<ActionResult> SetActive(int id, [FromBody] SetActiveBody req)
    {
        var result = await _accounts.SetActiveAsync(CurrentUserId, id, req.IsActive);
        return Respond(result);
    }

    [HttpPost("{id:int}/reset-password")]
    public async Task<ActionResult> ResetPassword(int id, [FromBody] ResetPasswordBody req)
    {
        var result = await _accounts.ResetPasswordAsync(id, req.NewPassword);
        return Respond(result);
    }
}

public record UpdateAccountBody(string FullName, string Email, AppRole Role, string? StudentCode = null);
public record SetActiveBody(bool IsActive);
public record ResetPasswordBody(string NewPassword);
