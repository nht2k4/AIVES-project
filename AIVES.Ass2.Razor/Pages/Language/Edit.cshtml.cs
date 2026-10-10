using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVES.Ass2.Razor.Pages.Language;

[Authorize(Roles = Roles.Admin + "," + Roles.Lecturer)]
public class EditModel(ILanguageConfigService languages) : TV5PageModel
{
    public LanguageConfigDto Config { get; private set; } = null!;
    [BindProperty] public AppLanguage SttLanguage { get; set; }
    [BindProperty] public AppLanguage TtsLanguage { get; set; }
    public bool IsAdmin => User.IsInRole(Roles.Admin);

    public async Task<IActionResult> OnGetAsync(int subjectId)
    {
        var failure = await LoadAsync(subjectId);
        if (failure is not null) return failure;
        SttLanguage = Config.SttLanguage;
        TtsLanguage = Config.TtsLanguage;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int subjectId)
    {
        // Load/authorize even for invalid forms and forged subject IDs.
        var failure = await LoadAsync(subjectId);
        if (failure is not null) return failure;
        if (!ModelState.IsValid) return Page();
        var result = await languages.UpdateAsync(CurrentUserId, new(subjectId, SttLanguage, TtsLanguage));
        if (!result.Succeeded)
        {
            failure = MapFailure(result);
            if (failure is not null) return failure;
            ModelState.AddModelError(string.Empty, result.Error!);
            return Page();
        }
        FlashSuccess("Đã cập nhật ngôn ngữ.");
        return IsAdmin ? RedirectToPage("/Subjects/Details", new { id = subjectId }) : RedirectToPage("/MySubjects/Index");
    }

    private async Task<IActionResult?> LoadAsync(int subjectId)
    {
        var result = await languages.GetForEditAsync(CurrentUserId, subjectId);
        if (!result.Succeeded) return MapFailure(result) ?? BadRequest();
        Config = result.Data!;
        return null;
    }
}
