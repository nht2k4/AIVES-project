using AIVES.Ass3.MVC.ViewModels;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using AIVES.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVES.Ass3.MVC.Controllers;

[Authorize(Roles = Roles.Admin + "," + Roles.Lecturer)]
public class VoicesController(IVoiceService voices, ISubjectService subjects, ILanguageConfigService languages) : AppController
{
    public async Task<IActionResult> Index(int? subjectId)
    {
        LanguageConfigDto? selected = null;
        if (subjectId is int id)
        {
            var result = await languages.GetForEditAsync(CurrentUserId, id);
            if (!result.Succeeded) return MapFailure(result) ?? BadRequest(result.Error);
            selected = result.Data;
        }

        var availableSubjects = User.IsInRole(Roles.Admin)
            ? await subjects.GetAllAsync()
            : await subjects.GetByLecturerAsync(CurrentUserId);
        return View(new VoicesViewModel(await voices.GetOverviewAsync(), availableSubjects, selected));
    }

    [HttpGet]
    public async Task<IActionResult> Preview(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 64) return BadRequest();

        var audio = await voices.OpenSpeechAsync("Xin chào. Đây là giọng đọc mẫu cho phòng thi vấn đáp AIVES.", name);
        if (audio is null) return StatusCode(StatusCodes.Status503ServiceUnavailable);

        Response.Headers.CacheControl = "no-store";
        return File(audio, "audio/pcm");
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(VoiceService.MaxClipBytes + 64 * 1024)]
    public async Task<IActionResult> Clone(string? name, string? description, IFormFile? clip, bool denoise)
    {
        if (clip is null || clip.Length is <= 0 or > VoiceService.MaxClipBytes)
        {
            FlashError("Chọn tệp mẫu giọng có dung lượng tối đa 20 MB.");
            return RedirectToAction(nameof(Index));
        }

        using var content = new MemoryStream((int)clip.Length);
        await clip.CopyToAsync(content, HttpContext.RequestAborted);
        var result = await voices.CloneAsync(CurrentUserId, name ?? "", description, content.ToArray(), clip.FileName, denoise);
        if (result.Succeeded) FlashSuccess("Đã clone giọng đọc.");
        else if (MapFailure(result) is { } failure) return failure;
        else FlashError(result.Error!);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return BadRequest();

        var result = await voices.DeleteAsync(CurrentUserId, name);
        if (result.Succeeded) FlashSuccess("Đã xóa giọng clone. Các môn dùng giọng này đã về giọng mặc định.");
        else if (MapFailure(result) is { } failure) return failure;
        else FlashError(result.Error!);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int subjectId, string? name)
    {
        var result = await languages.GetForEditAsync(CurrentUserId, subjectId);
        if (!result.Succeeded) return MapFailure(result) ?? BadRequest(result.Error);

        var config = result.Data!;
        var selectedVoice = name?.Trim() ?? "";
        if (string.Equals(selectedVoice, config.TtsVoice ?? "", StringComparison.OrdinalIgnoreCase))
        {
            FlashSuccess("Môn đang dùng giọng đã chọn.");
            return RedirectToAction(nameof(Index), new { subjectId });
        }

        if (selectedVoice.Length > 0)
        {
            var overview = await voices.GetOverviewAsync();
            var available = overview.Voices.FirstOrDefault(v => string.Equals(v.Name, selectedVoice, StringComparison.OrdinalIgnoreCase));
            if (available is null)
            {
                FlashError("Giọng đã chọn không có trong danh sách.");
                return RedirectToAction(nameof(Index), new { subjectId });
            }
            selectedVoice = available.Name;
        }

        var updated = await languages.UpdateAsync(CurrentUserId,
            new UpdateLanguageRequest(subjectId, config.SttLanguage, config.TtsLanguage, selectedVoice));
        if (updated.Succeeded) FlashSuccess("Đã cập nhật giọng đọc cho môn.");
        else if (MapFailure(updated) is { } failure) return failure;
        else FlashError(updated.Error!);

        return RedirectToAction(nameof(Index), new { subjectId });
    }
}
