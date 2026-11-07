namespace AIVES.Business.Interfaces;

public record TtsStatus(bool Online, string? Backend, int SampleRate, int MaxStreams, int Active);

public record TtsVoiceInfo(string Name, string Description, string? Gender, bool Featured);

// Cổng vào máy chủ VieNeu-TTS cục bộ. Mọi hàm đều không ném lỗi khi máy chủ tắt: trả về Online=false hoặc null.
public interface ITextToSpeech
{
    Task<TtsStatus> GetStatusAsync(CancellationToken ct = default);

    // Các giọng có sẵn của VieNeu, null nếu máy chủ không chạy
    Task<List<TtsVoiceInfo>?> GetVoicesAsync(CancellationToken ct = default);

    // Nạp giọng clone từ file mẫu thu. Trả về thông báo lỗi, hoặc null nếu thành công.
    Task<string?> EnrollVoiceAsync(string name, byte[] clip, string fileName, bool denoise, string? description, CancellationToken ct = default);

    // Luồng PCM s16le 48 kHz mono, phát được ngay khi byte đầu tiên tới. Null nếu máy chủ tắt hoặc từ chối.
    Task<Stream?> OpenSpeechAsync(string text, string? voice, CancellationToken ct = default);
}
