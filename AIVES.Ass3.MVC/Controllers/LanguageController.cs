using AIVES.Ass3.MVC.ViewModels;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVES.Ass3.MVC.Controllers;

// TV5 - Ass3 - việc 5.2: Ngôn ngữ STT/TTS + chỉnh tay cho phòng thi.
// Admin và Giảng viên đều vào được; quyền theo môn do ILanguageConfigService quyết định (giảng viên gõ tay subjectId môn khác vẫn bị chặn).
[Authorize(Roles = Roles.Admin + "," + Roles.Lecturer)]
public class LanguageController : AppController
{
    private readonly ILanguageConfigService _languages;

    public LanguageController(ILanguageConfigService languages)
    {
        _languages = languages;
    }

    public async Task<IActionResult> Edit(int subjectId)
    {
        var model = new LanguageEditViewModel();
        var failure = await LoadAsync(subjectId, model);
        if (failure is not null) return failure;

        model.SttLanguage = model.Config.SttLanguage;
        model.TtsLanguage = model.Config.TtsLanguage;
        model.SttTerms = model.Config.SttTerms;
        model.AiTimeoutSeconds = model.Config.AiTimeoutSeconds;
        model.UseExternalAi = model.Config.UseExternalAi;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int subjectId, LanguageEditViewModel model)
    {
        // Luôn nạp (và kiểm quyền) trước, kể cả khi form sai hoặc subjectId bị sửa tay
        var failure = await LoadAsync(subjectId, model);
        if (failure is not null) return failure;
        if (!ModelState.IsValid) return View(model);

        // TtsVoice = null: giữ giọng hiện tại (giọng đổi ở trang Giọng đọc). Ô từ điển để trống = xóa từ điển.
        var request = new UpdateLanguageRequest(subjectId, model.SttLanguage, model.TtsLanguage, TtsVoice: null,
            SttTerms: model.SttTerms ?? string.Empty, AiTimeoutSeconds: model.AiTimeoutSeconds, UseExternalAi: model.UseExternalAi);
        var result = await _languages.UpdateAsync(CurrentUserId, request);
        if (!result.Succeeded)
        {
            failure = MapFailure(result);
            if (failure is not null) return failure;
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        FlashSuccess("Đã cập nhật ngôn ngữ.");
        return User.IsInRole(Roles.Admin)
            ? RedirectToAction("Details", "Subjects", new { id = subjectId })
            : RedirectToAction("Index", "MySubjects");
    }

    private async Task<IActionResult?> LoadAsync(int subjectId, LanguageEditViewModel model)
    {
        var result = await _languages.GetForEditAsync(CurrentUserId, subjectId);
        if (!result.Succeeded) return MapFailure(result) ?? BadRequest();
        model.Config = result.Data!;
        return null;
    }
}
