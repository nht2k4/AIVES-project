using AIVES.Ass2.Razor.Infrastructure;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;

namespace AIVES.Ass2.Razor.Pages.Exams;

// Quyền Staff được cấu hình cho thư mục /Exams trong Program.cs.
public class IndexModel(IExamService exams) : AppPageModel
{
    public IReadOnlyList<ExamListItemDto> Exams { get; private set; } = Array.Empty<ExamListItemDto>();

    public async Task OnGetAsync()
    {
        // Service lọc các môn theo quyền của tài khoản đang đăng nhập.
        Exams = await exams.GetListAsync(CurrentUserId);
    }
}
