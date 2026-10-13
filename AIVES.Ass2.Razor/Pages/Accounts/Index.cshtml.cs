using AIVES.Ass2.Razor.Infrastructure;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVES.Ass2.Razor.Pages.Accounts;

// TV4 - Ass2 - việc 4.2: danh sách tài khoản, lọc vai trò (gồm Chờ cấp quyền), khóa/mở khóa.
[Authorize(Roles = Roles.Admin)]
public class IndexModel(IAccountService accounts) : AppPageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Keyword { get; set; }

    [BindProperty(SupportsGet = true)]
    public AppRole? Role { get; set; }

    public IReadOnlyList<AccountDto> Accounts { get; private set; } = Array.Empty<AccountDto>();

    public async Task OnGetAsync() =>
        Accounts = await accounts.SearchAsync(Keyword, Role);

    public async Task<IActionResult> OnPostSetActiveAsync(int id, bool isActive)
    {
        var result = await accounts.SetActiveAsync(CurrentUserId, id, isActive);
        if (!result.Succeeded)
        {
            var mapped = MapFailure(result);
            if (mapped is not null) return mapped;
            FlashError(result.Error!);
            return RedirectToPage(new { Keyword, Role });
        }

        FlashSuccess(isActive ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.");
        return RedirectToPage(new { Keyword, Role });
    }
}
