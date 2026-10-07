using AIVES.DataAccess.Entities;

namespace AIVES.DataAccess.Repositories.Interfaces;

public interface IVoiceRepository
{
    // Không nạp cột Clip (có thể vài MB) khi chỉ cần liệt kê
    Task<List<Voice>> GetAllAsync();
    Task<Voice?> GetByNameAsync(string name);
    Task AddAsync(Voice voice);
    Task<bool> RemoveAsync(string name);
}
