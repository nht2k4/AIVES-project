using AIVES.DataAccess.Entities;

namespace AIVES.DataAccess.Repositories.Interfaces;

public interface IExamRepository
{
    // subjectIds = null: tất cả môn (Admin). Có Include Subject + ExamParticipants.
    Task<List<ExamSession>> GetListAsync(IReadOnlyCollection<int>? subjectIds);

    // Có theo dõi thay đổi (tracked) để Service sửa rồi gọi SaveAsync
    Task<ExamSession?> GetDetailAsync(int id);

    // Các lượt thi của một sinh viên (theo MSSV), có Include Session.Subject
    Task<List<ExamParticipant>> GetParticipantsByStudentCodeAsync(string studentCode);

    Task AddAsync(ExamSession session);
    Task SaveAsync();
}
