using AIVES.Business.Common;
using AIVES.Business.DTOs;

namespace AIVES.Business.Interfaces;

public interface IExamService
{
    // Admin thấy mọi phiên thi, giảng viên chỉ thấy phiên của môn được phân công
    Task<List<ExamListItemDto>> GetListAsync(int currentUserId);

    // Các môn đang mở mà người dùng được tạo phiên thi
    Task<List<SubjectDto>> GetSubjectsForExamAsync(int currentUserId);

    Task<ServiceResult<int>> CreateAsync(int currentUserId, CreateExamRequest request);
    Task<ServiceResult<ExamDetailDto>> GetDetailAsync(int currentUserId, int examId);
    Task<ServiceResult> SetOpenAsync(int currentUserId, int examId, bool open);

    // Chọn lại bộ câu hỏi cho toàn bộ thí sinh (chỉ khi chưa ai bắt đầu thi)
    Task<ServiceResult> ReallocateAsync(int currentUserId, int examId);
}
