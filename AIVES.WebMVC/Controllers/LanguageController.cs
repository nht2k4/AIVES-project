using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using AIVES.WebMVC.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVES.WebMVC.Controllers;

// Cả Admin và Giảng viên đều vào được. Ai được sửa môn nào thì tầng Business quyết định.
[Authorize]
public class LanguageController : AppController
{
    private readonly ILanguageConfigService _languageService;

    public LanguageController(ILanguageConfigService languageService) => _languageService = languageService;

    [HttpGet]
    public async Task<IActionResult> Edit(int subjectId)
    {
        var result = await _languageService.GetForEditAsync(CurrentUserId, subjectId);
        if (!result.Succeeded) return MapFailure(result) ?? BadRequest();

        var c = result.Data!;
        return View(new LanguageConfigViewModel
        {
            SubjectId = c.SubjectId,
            SubjectCode = c.SubjectCode,
            SubjectName = c.SubjectName,
            SttLanguage = c.SttLanguage,
            TtsLanguage = c.TtsLanguage,
            UpdatedAt = c.UpdatedAt,
            UpdatedBy = c.UpdatedBy
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(LanguageConfigViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _languageService.UpdateAsync(
            CurrentUserId, new UpdateLanguageRequest(model.SubjectId, model.SttLanguage, model.TtsLanguage));

        if (!result.Succeeded)
        {
            var mapped = MapFailure(result);
            if (mapped is not null) return mapped;

            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        FlashSuccess($"Đã lưu cấu hình ngôn ngữ cho {model.SubjectCode}.");
        return User.IsInRole(Roles.Admin)
            ? RedirectToAction("Details", "Subjects", new { id = model.SubjectId })
            : RedirectToAction("Index", "MySubjects");
    }

    // Endpoint minh họa "hợp đồng" cho chức năng STT/TTS: /Language/Speech?subjectId=1
    [HttpGet]
    public async Task<IActionResult> Speech(int subjectId)
    {
        var result = await _languageService.GetSpeechConfigAsync(subjectId);
        if (!result.Succeeded) return MapFailure(result) ?? BadRequest();
        return Json(result.Data);
    }
}
