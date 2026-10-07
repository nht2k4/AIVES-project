using AIVES.Business.Common;
using AIVES.Business.DTOs;

namespace AIVES.Business.Interfaces;

public interface IInterviewService
{
    // Bắt đầu hoặc tiếp tục phỏng vấn: gọi lại nhiều lần vẫn trả về đúng lượt hỏi đang mở
    Task<ServiceResult<InterviewStateDto>> StartAsync(int currentUserId, int participantId);

    // Nộp câu trả lời (đã là văn bản sau STT). Trả về lượt hỏi kế tiếp: câu hỏi xoáy, câu chính tiếp theo, hoặc kết thúc.
    Task<ServiceResult<InterviewStateDto>> SubmitAnswerAsync(int currentUserId, int turnId, string? answer, bool timedOut);

    // Các lượt thi của sinh viên đang đăng nhập (khớp theo mã sinh viên của tài khoản)
    Task<List<MyInterviewDto>> GetMyInterviewsAsync(int currentUserId);

    // Giọng đọc (luồng PCM 48 kHz) của một câu hỏi bằng VieNeu-TTS cục bộ. Lỗi Validation = máy chủ TTS không chạy.
    Task<ServiceResult<Stream>> SpeakAsync(int currentUserId, int turnId);

    Task<ServiceResult<TranscriptDto>> GetTranscriptAsync(int currentUserId, int participantId);
}
