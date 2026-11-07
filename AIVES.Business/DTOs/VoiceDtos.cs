using AIVES.Business.Interfaces;

namespace AIVES.Business.DTOs;

// Cloned = giọng do người dùng clone (lưu trong database); còn lại là giọng có sẵn của VieNeu
public record VoiceDto(string Name, string Description, string? Gender, bool Featured, bool Cloned);

public record VoiceOverviewDto(TtsStatus Status, string DefaultVoice, IReadOnlyList<VoiceDto> Voices);
