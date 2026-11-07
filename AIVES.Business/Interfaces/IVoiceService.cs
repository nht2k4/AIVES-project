using AIVES.Business.Common;
using AIVES.Business.DTOs;

namespace AIVES.Business.Interfaces;

public interface IVoiceService
{
    Task<VoiceOverviewDto> GetOverviewAsync();

    // Chỉ quản trị viên. Mẫu thu 3-8 giây, ghi âm sạch của chính người dùng hoặc người đã đồng ý.
    Task<ServiceResult> CloneAsync(int currentUserId, string name, string? description, byte[] clip, string fileName, bool denoise);
    Task<ServiceResult> DeleteAsync(int currentUserId, string name);

    // Đọc văn bản bằng giọng đã chọn (tự nạp lại giọng clone nếu VieNeu vừa khởi động lại). Null = TTS không chạy.
    Task<Stream?> OpenSpeechAsync(string text, string? voice);

    string DefaultVoice { get; }
}
