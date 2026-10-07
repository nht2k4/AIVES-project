using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;

namespace AIVES.Tests.Support;

// Máy chủ giọng đọc giả: bật/tắt được, nhớ các giọng đã nạp, đếm số lần được gọi.
public class FakeTextToSpeech : ITextToSpeech
{
    public bool Online { get; set; } = true;
    public HashSet<string> BuiltIn { get; } = new() { "Mai Anh", "Hải Đăng" };
    public Dictionary<string, byte[]> Enrolled { get; } = new();
    public List<string?> SpokenWith { get; } = new();
    public int EnrollCalls { get; private set; }

    public Task<TtsStatus> GetStatusAsync(CancellationToken ct = default) =>
        Task.FromResult(new TtsStatus(Online, Online ? "onnx" : null, 48000, 1, 0));

    public Task<List<TtsVoiceInfo>?> GetVoicesAsync(CancellationToken ct = default) =>
        Task.FromResult<List<TtsVoiceInfo>?>(!Online ? null
            : BuiltIn.Concat(Enrolled.Keys).Select(n => new TtsVoiceInfo(n, "", null, false)).ToList());

    public Task<string?> EnrollVoiceAsync(string name, byte[] clip, string fileName, bool denoise, string? description, CancellationToken ct = default)
    {
        EnrollCalls++;
        if (!Online) return Task.FromResult<string?>("VieNeu-TTS chưa chạy.");
        if (BuiltIn.Contains(name)) return Task.FromResult<string?>($"'{name}' là giọng có sẵn.");

        Enrolled[name] = clip;
        return Task.FromResult<string?>(null);
    }

    public Task<Stream?> OpenSpeechAsync(string text, string? voice, CancellationToken ct = default)
    {
        SpokenWith.Add(voice);
        var known = voice is null || BuiltIn.Contains(voice) || Enrolled.ContainsKey(voice);
        return Task.FromResult<Stream?>(Online && known ? new MemoryStream(new byte[] { 1, 2, 3, 4 }) : null);
    }
}

// AI hỏi xoáy giả: test quyết định "AI" trả lời gì.
public class ScriptedFollowUp : IFollowUpGenerator
{
    public Func<FollowUpContext, FollowUpDecision> Decide { get; set; } = _ => new FollowUpDecision(false, null, null);
    public List<FollowUpContext> Calls { get; } = new();

    public Task<FollowUpDecision> DecideAsync(FollowUpContext context, CancellationToken ct = default)
    {
        Calls.Add(context);
        return Task.FromResult(Decide(context));
    }

    public static ScriptedFollowUp Always(string question = "Bạn giải thích rõ hơn được không?") =>
        new() { Decide = _ => new FollowUpDecision(true, question, "Câu trả lời còn mơ hồ.") };
}

// Dịch vụ ngôn ngữ giả cho test phỏng vấn: luôn trả về cấu hình cố định.
public class StubLanguageConfig : ILanguageConfigService
{
    public SpeechConfig Config { get; set; } = new("PRN222", "vi-VN", "vi-VN", null);

    public Task<ServiceResult<LanguageConfigDto>> GetForEditAsync(int currentUserId, int subjectId) =>
        throw new NotSupportedException();

    public Task<ServiceResult> UpdateAsync(int currentUserId, UpdateLanguageRequest request) =>
        throw new NotSupportedException();

    public Task<ServiceResult<SpeechConfig>> GetSpeechConfigAsync(int subjectId) =>
        Task.FromResult(ServiceResult<SpeechConfig>.Ok(Config));
}

// Dịch vụ giọng đọc giả cho test phỏng vấn.
public class StubVoiceService : IVoiceService
{
    public bool Available { get; set; } = true;
    public List<string?> VoicesUsed { get; } = new();
    public string DefaultVoice => "Mai Anh";

    public Task<VoiceOverviewDto> GetOverviewAsync() => throw new NotSupportedException();

    public Task<ServiceResult> CloneAsync(int currentUserId, string name, string? description, byte[] clip, string fileName, bool denoise) =>
        throw new NotSupportedException();

    public Task<ServiceResult> DeleteAsync(int currentUserId, string name) => throw new NotSupportedException();

    public Task<Stream?> OpenSpeechAsync(string text, string? voice)
    {
        VoicesUsed.Add(voice);
        return Task.FromResult<Stream?>(Available ? new MemoryStream(new byte[] { 1, 2 }) : null);
    }
}

// Băm "giả" nhưng đủ để test: cùng mật khẩu thì cùng chuỗi, khác mật khẩu thì khác chuỗi, và chuỗi băm khác mật khẩu gốc.
public class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => "fake:" + new string(password.Reverse().ToArray());

    public bool Verify(string password, string passwordHash) => Hash(password) == passwordHash;
}
