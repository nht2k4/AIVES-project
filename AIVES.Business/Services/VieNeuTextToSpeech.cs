using AIVES.Business.Interfaces;
using AIVES.Business.Models;

namespace AIVES.Business.Services;

// TV2 - Ass3 - việc 2.3. Cổng gọi máy chủ VieNeu-TTS chạy cục bộ (API tương thích OpenAI). Xem AIVES_PhanCong_Ass2_Ass3.md.
// Lớp này gọi mạng nên KHÔNG có test tự động: kiểm bằng checklist trong tài liệu.
public class VieNeuTextToSpeech : ITextToSpeech
{
    public VieNeuTextToSpeech(TtsOptions options)
    {
    }

    public Task<TtsStatus> GetStatusAsync(CancellationToken ct = default) => throw new NotImplementedException();

    public Task<List<TtsVoiceInfo>?> GetVoicesAsync(CancellationToken ct = default) => throw new NotImplementedException();

    public Task<string?> EnrollVoiceAsync(string name, byte[] clip, string fileName, bool denoise, string? description, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<Stream?> OpenSpeechAsync(string text, string? voice, CancellationToken ct = default) => throw new NotImplementedException();
}
