using AIVES.Business.Common;

namespace AIVES.Business.Interfaces;

public interface IAssignmentService
{
    Task<ServiceResult> AssignAsync(int lecturerId, int subjectId);
    Task<ServiceResult> UnassignAsync(int lecturerId, int subjectId);

    // Điểm kiểm tra quyền duy nhất của hệ thống: các chức năng 1-6 gọi hàm này để biết giảng viên có quyền trên môn không
    Task<bool> CanAccessSubjectAsync(int accountId, int subjectId);
}
