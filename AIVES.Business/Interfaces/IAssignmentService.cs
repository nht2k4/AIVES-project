using AIVES.Business.Common;

namespace AIVES.Business.Interfaces;

public interface IAssignmentService
{
    Task<ServiceResult> AssignAsync(int lecturerId, int subjectId);
    Task<ServiceResult> UnassignAsync(int lecturerId, int subjectId);

    // Điểm kiểm tra quyền duy nhất của hệ thống: các nhóm chức năng khác cũng gọi hàm này
    Task<bool> CanAccessSubjectAsync(int accountId, int subjectId);
}
