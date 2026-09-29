using AIVES.Business.Models;

namespace AIVES.Business.DTOs;

public record LanguageConfigDto(
    int SubjectId, string SubjectCode, string SubjectName,
    AppLanguage SttLanguage, AppLanguage TtsLanguage,
    DateTime? UpdatedAt, string? UpdatedBy);

public record UpdateLanguageRequest(int SubjectId, AppLanguage SttLanguage, AppLanguage TtsLanguage);

// "Hợp đồng" cho các nhóm chức năng khác (ghi âm, STT, TTS) dùng
public record SpeechConfig(string SubjectCode, string SttLocale, string TtsLocale);
