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

    // Lưu ghi âm câu trả lời của một lượt: chỉ chính sinh viên dự thi, sau khi đã nộp câu trả lời, mỗi lượt một lần,
    // tệp âm thanh (audio/...) không rỗng và tối đa 10 MB.
    Task<ServiceResult> SaveRecordingAsync(int currentUserId, int turnId, byte[] audio, string? contentType);

    // Nghe lại ghi âm để đối chiếu với văn bản: chỉ Admin và giảng viên phụ trách môn (sinh viên không tải được).
    Task<ServiceResult<RecordingDto>> GetRecordingAsync(int currentUserId, int turnId);
}
