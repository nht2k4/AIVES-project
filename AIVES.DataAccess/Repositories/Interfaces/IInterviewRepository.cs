using AIVES.DataAccess.Entities;

namespace AIVES.DataAccess.Repositories.Interfaces;

// Các hàm Get trả về entity đang được theo dõi: Service sửa nhiều thứ rồi gọi SaveAsync một lần để lượt hỏi + trạng thái thí sinh luôn đồng bộ
public interface IInterviewRepository
{
    Task<ExamParticipant?> GetParticipantAsync(int participantId);
    Task<InterviewTurn?> GetTurnAsync(int turnId);
    Task<List<InterviewTurn>> GetTurnsAsync(int participantId);
    Task<bool> HasTurnsAsync(int participantId);
    void AddTurn(InterviewTurn turn);

    // Ghi âm câu trả lời: một bản ghi mỗi lượt hỏi. GetRecordingAsync nạp cả nội dung âm thanh; GetRecordedTurnIdsAsync chỉ lấy mã lượt (nhẹ, cho biên bản).
    Task<TurnRecording?> GetRecordingAsync(int turnId);
    Task<List<int>> GetRecordedTurnIdsAsync(int participantId);
    void AddRecording(TurnRecording recording);

    Task SaveAsync();
}
