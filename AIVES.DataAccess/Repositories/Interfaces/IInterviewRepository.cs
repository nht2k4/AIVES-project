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
    Task SaveAsync();
}
