using AIVES.Business.Models;

namespace AIVES.Business.DTOs;

public record LanguageConfigDto(
    int SubjectId, string SubjectCode, string SubjectName,
    AppLanguage SttLanguage, AppLanguage TtsLanguage,
    DateTime? UpdatedAt, string? UpdatedBy);

public record UpdateLanguageRequest(int SubjectId, AppLanguage SttLanguage, AppLanguage TtsLanguage);

// "Hợp đồng" cho Chức năng 3 (Lõi phỏng vấn AI): ngôn ngữ STT/TTS của buổi thi theo môn
public record SpeechConfig(string SubjectCode, string SttLocale, string TtsLocale);
