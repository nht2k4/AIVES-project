using AIVES.DataAccess.Entities;

namespace AIVES.DataAccess.Repositories.Interfaces;

public interface IQuestionRepository
{
    Task<List<Question>> GetBySubjectAsync(int subjectId, bool onlyActive);
    Task<Question?> GetByIdAsync(int id);
    Task AddAsync(Question question);
    Task UpdateAsync(Question question);
}
