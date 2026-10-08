using AIVES.Business.Models;

namespace AIVES.Business.DTOs;

public record LanguageConfigDto(
    int SubjectId, string SubjectCode, string SubjectName,
    AppLanguage SttLanguage, AppLanguage TtsLanguage,
    DateTime? UpdatedAt, string? UpdatedBy, string? TtsVoice = null,
    string? SttTerms = null, int AiTimeoutSeconds = 8, bool UseExternalAi = true);

// Các thông số chỉnh tay theo môn. Mọi trường sau ngôn ngữ: null = giữ nguyên.
// TtsVoice: tên giọng VieNeu-TTS, chuỗi rỗng = giọng mặc định.
// SttTerms: từ điển thuật ngữ, mỗi dòng "cách STT hay nghe sai => cách viết đúng", chuỗi rỗng = xóa từ điển.
// AiTimeoutSeconds: thời gian chờ AI tối đa (2 đến 30 giây), quá thì dùng luật dự phòng để giữ nhịp vấn đáp.
// UseExternalAi: false = không gửi câu trả lời của sinh viên ra dịch vụ AI bên ngoài (quyền riêng tư).
public record UpdateLanguageRequest(int SubjectId, AppLanguage SttLanguage, AppLanguage TtsLanguage, string? TtsVoice = null,
    string? SttTerms = null, int? AiTimeoutSeconds = null, bool? UseExternalAi = null);

// "Hợp đồng" cho Chức năng 3 (Lõi phỏng vấn AI): cấu hình giọng nói và AI của buổi thi theo môn
public record SpeechConfig(string SubjectCode, string SttLocale, string TtsLocale, string? TtsVoice = null,
    string? SttTerms = null, int AiTimeoutSeconds = 8, bool UseExternalAi = true);
