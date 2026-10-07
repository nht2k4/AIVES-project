using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.Business.Services;

// TV2 - Ass3 - việc 2.4. Luật quản lý giọng đọc (clone, xóa, đọc có dự phòng). Xem AIVES.Tests/Business/AiAndVoiceTests.cs và AIVES_PhanCong_Ass2_Ass3.md.
public class VoiceService : IVoiceService
{
    public VoiceService(ITextToSpeech tts, IVoiceRepository voices, IAccountRepository accounts, TtsOptions options)
    {
    }

    public string DefaultVoice => throw new NotImplementedException();

    public Task<VoiceOverviewDto> GetOverviewAsync() => throw new NotImplementedException();

    public Task<ServiceResult> CloneAsync(int currentUserId, string name, string? description, byte[] clip, string fileName, bool denoise) =>
        throw new NotImplementedException();

    public Task<ServiceResult> DeleteAsync(int currentUserId, string name) => throw new NotImplementedException();

    public Task<Stream?> OpenSpeechAsync(string text, string? voice) => throw new NotImplementedException();
}
