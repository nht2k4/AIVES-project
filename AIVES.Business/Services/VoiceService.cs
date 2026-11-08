using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.Business.Services;

// TV2 - Ass3 - việc 2.4. Luật quản lý giọng đọc (clone, xóa, đọc có dự phòng). Xem AIVES.Tests/Business/AiAndVoiceTests.cs và AIVES_PhanCong_Ass2_Ass3.md.
public class VoiceService : IVoiceService
{
    public const int MaxClipBytes = 20 * 1024 * 1024;
    private static readonly Regex Label = new(@"\A[\p{L}\p{Nd}][\p{L}\p{Nd} ._-]{0,63}\z");
    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".wav", ".mp3", ".flac", ".ogg", ".m4a"
    };
    // VieNeu có thể ghi đè giọng clone. Tuần tự hóa các thao tác nạp và xóa trong ứng dụng.
    private static readonly SemaphoreSlim VoiceChanges = new(1);
    // VieNeu giữ giọng đã nạp trong bộ nhớ và API hiện tại chỉ có thao tác nạp.
    private static readonly ConcurrentDictionary<string, byte> HiddenVoices = new(StringComparer.OrdinalIgnoreCase);
    private readonly ITextToSpeech _tts;
    private readonly IVoiceRepository _voices;
    private readonly IAccountRepository _accounts;

    public VoiceService(ITextToSpeech tts, IVoiceRepository voices, IAccountRepository accounts, TtsOptions options)
    {
        _tts = tts;
        _voices = voices;
        _accounts = accounts;
        DefaultVoice = options.Voice;
    }

    public string DefaultVoice { get; }

    public async Task<VoiceOverviewDto> GetOverviewAsync()
    {
        var status = await _tts.GetStatusAsync();
        var presets = status.Online ? await _tts.GetVoicesAsync() : null;
        var voices = new Dictionary<string, VoiceDto>(StringComparer.OrdinalIgnoreCase);
        if (presets is not null)
        {
            foreach (var preset in presets)
            {
                if (!HiddenVoices.ContainsKey(preset.Name))
                    voices[preset.Name] = new VoiceDto(preset.Name, preset.Description, preset.Gender, preset.Featured, false);
            }
        }

        foreach (var clone in await _voices.GetAllAsync())
            voices[clone.Name] = new VoiceDto(clone.Name, clone.Description ?? "", null, false, true);

        return new VoiceOverviewDto(status, DefaultVoice, voices.Values
            .OrderByDescending(v => v.Featured).ThenBy(v => v.Name, StringComparer.OrdinalIgnoreCase).ToList());
    }

    public async Task<ServiceResult> CloneAsync(int currentUserId, string name, string? description, byte[] clip,
        string fileName, bool denoise)
    {
        if (!await IsAdminAsync(currentUserId))
            return ServiceResult.Forbidden("Chỉ quản trị viên được tạo giọng.");

        name = name.Trim().Normalize(NormalizationForm.FormC);
        description = description?.Trim().Normalize(NormalizationForm.FormC);
        if (!Label.IsMatch(name))
            return ServiceResult.Fail("Tên giọng dài 1–64 ký tự, bắt đầu bằng chữ hoặc số và chỉ dùng chữ, số, dấu cách, chấm, gạch ngang hoặc gạch dưới.");
        if (!string.IsNullOrEmpty(description) && !Label.IsMatch(description))
            return ServiceResult.Fail("Mô tả tối đa 64 ký tự và dùng cùng quy tắc với tên giọng.");

        fileName = Path.GetFileName(fileName.Replace('\\', '/'));
        if (clip.Length is 0 or > MaxClipBytes || fileName.Length > 100 || fileName.Any(char.IsControl)
            || !AudioExtensions.Contains(Path.GetExtension(fileName)))
            return ServiceResult.Fail("Chọn tệp WAV, MP3, FLAC, OGG hoặc M4A có dung lượng tối đa 20 MB.");

        await VoiceChanges.WaitAsync();
        try
        {
            if (await _voices.GetByNameAsync(name) is not null)
                return ServiceResult.Fail("Tên giọng đã tồn tại.");

            var presets = await _tts.GetVoicesAsync();
            if (!HiddenVoices.ContainsKey(name) && presets?.Any(v => string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase)) == true)
                return ServiceResult.Fail("Tên giọng đã được VieNeu-TTS sử dụng.");

            var error = await _tts.EnrollVoiceAsync(name, clip, fileName, denoise, description);
            if (error is not null) return ServiceResult.Fail(error);
            HiddenVoices.TryAdd(name, 0);

            await _voices.AddAsync(new Voice
            {
                Name = name,
                Description = string.IsNullOrEmpty(description) ? null : description,
                Clip = clip,
                FileName = fileName,
                Denoise = denoise,
                CreatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow
            });
            HiddenVoices.TryRemove(name, out _);
            return ServiceResult.Ok();
        }
        finally
        {
            VoiceChanges.Release();
        }
    }

    public async Task<ServiceResult> DeleteAsync(int currentUserId, string name)
    {
        if (!await IsAdminAsync(currentUserId))
            return ServiceResult.Forbidden("Chỉ quản trị viên được xóa giọng.");

        await VoiceChanges.WaitAsync();
        try
        {
            name = name.Trim();
            if (!await _voices.RemoveAsync(name))
                return ServiceResult.NotFound("Không tìm thấy giọng đã clone.");

            HiddenVoices.TryAdd(name, 0);
            return ServiceResult.Ok();
        }
        finally
        {
            VoiceChanges.Release();
        }
    }

    public async Task<Stream?> OpenSpeechAsync(string text, string? voice)
    {
        var requested = string.IsNullOrWhiteSpace(voice) ? DefaultVoice : voice.Trim();
        if (HiddenVoices.ContainsKey(requested)) requested = DefaultVoice;
        var speech = await _tts.OpenSpeechAsync(text, requested);
        if (speech is not null || string.Equals(requested, DefaultVoice, StringComparison.OrdinalIgnoreCase))
            return speech;

        // Máy chủ có thể đã khởi động lại và quên giọng clone.
        await VoiceChanges.WaitAsync();
        try
        {
            var clone = await _voices.GetByNameAsync(requested);
            if (clone is not null &&
                await _tts.EnrollVoiceAsync(clone.Name, clone.Clip, clone.FileName, clone.Denoise, clone.Description) is null)
            {
                speech = await _tts.OpenSpeechAsync(text, clone.Name);
            }
        }
        finally
        {
            VoiceChanges.Release();
        }

        return speech ?? await _tts.OpenSpeechAsync(text, DefaultVoice);
    }

    private async Task<bool> IsAdminAsync(int accountId) =>
        await _accounts.GetByIdAsync(accountId) is { IsActive: true, Role: Roles.Admin };
}
