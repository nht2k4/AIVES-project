using AIVES.DataAccess.Entities;

namespace AIVES.DataAccess.Repositories.Interfaces;

public interface IExamRepository
{
    // subjectIds = null: tất cả môn (Admin). Có Include Subject + ExamParticipants.
    Task<List<ExamSession>> GetListAsync(IReadOnlyCollection<int>? subjectIds);

    // Có theo dõi thay đổi (tracked) để Service sửa rồi gọi SaveAsync
    Task<ExamSession?> GetDetailAsync(int id);

    Task AddAsync(ExamSession session);
    Task SaveAsync();
}
