using System.ComponentModel.DataAnnotations;
using System.Globalization;
using AIVES.Ass2.Razor.Infrastructure;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace AIVES.Ass2.Razor.Pages.Exams;

// Policy Staff áp dụng cho toàn bộ /Exams trong Program.cs.
public class CreateModel(IExamService exams, IHubContext<LiveHub> live) : AppPageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<SubjectDto> Subjects { get; private set; } = Array.Empty<SubjectDto>();
    public string MinimumStartTime => DateTime.Now.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);

    public class InputModel
    {
        [Required(ErrorMessage = "Chọn môn học.")]
        [Range(1, int.MaxValue, ErrorMessage = "Chọn môn học hợp lệ.")]
        [Display(Name = "Môn học")]
        public int? SubjectId { get; set; }

        [Required(ErrorMessage = "Nhập tên phiên thi.")]
        [StringLength(150, ErrorMessage = "Tên phiên thi tối đa 150 ký tự.")]
        [Display(Name = "Tên phiên thi")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Chọn thời gian bắt đầu.")]
        [Display(Name = "Thời gian bắt đầu")]
        public DateTime? StartTime { get; set; }

        [Required(ErrorMessage = "Nhập khung thời gian.")]
        [Range(1, 120, ErrorMessage = "Khung thời gian từ 1 đến 120 phút.")]
        [Display(Name = "Khung thời gian mỗi thí sinh (phút)")]
        public int? SlotMinutes { get; set; } = 10;

        [Required(ErrorMessage = "Nhập số câu hỏi chính.")]
        [Range(1, 20, ErrorMessage = "Số câu hỏi chính từ 1 đến 20.")]
        [Display(Name = "Số câu hỏi chính")]
        public int? MainQuestionCount { get; set; } = 3;

        [Required(ErrorMessage = "Nhập tổng số câu đào sâu.")]
        [Range(0, 40, ErrorMessage = "Tổng số câu đào sâu từ 0 đến 40.")]
        [Display(Name = "Tổng số câu đào sâu mỗi thí sinh")]
        public int? MaxFollowUps { get; set; } = 4;

        [Required(ErrorMessage = "Nhập số lượt hỏi xoáy mỗi câu.")]
        [Range(0, 5, ErrorMessage = "Số lượt hỏi xoáy mỗi câu từ 0 đến 5.")]
        [Display(Name = "Số lượt hỏi xoáy tối đa mỗi câu")]
        public int? MaxFollowUpsPerQuestion { get; set; } = 2;

        [Required(ErrorMessage = "Nhập thời gian trả lời.")]
        [Range(10, 600, ErrorMessage = "Thời gian trả lời từ 10 đến 600 giây.")]
        [Display(Name = "Thời gian trả lời mỗi câu (giây)")]
        public int? AnswerSeconds { get; set; } = 60;

        [Required(ErrorMessage = "Nhập danh sách sinh viên.")]
        [Display(Name = "Danh sách sinh viên")]
        public string StudentList { get; set; } = string.Empty;
    }

    public async Task OnGetAsync()
    {
        Input.StartTime = DateTime.Today.AddDays(1).AddHours(8);
        await LoadSubjectsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Nạp lại lựa chọn khi trả về form lỗi; không ghi đè dữ liệu người dùng đã nhập.
        await LoadSubjectsAsync();
        if (!ModelState.IsValid) return Page();

        // Service kiểm tra lại thời gian, danh sách sinh viên, quyền truy cập và ngân hàng câu hỏi.
        var result = await exams.CreateAsync(CurrentUserId, new CreateExamRequest(
            Input.SubjectId!.Value, Input.Title, Input.StartTime!.Value, Input.SlotMinutes!.Value,
            Input.MainQuestionCount!.Value, Input.MaxFollowUps!.Value,
            Input.MaxFollowUpsPerQuestion!.Value, Input.AnswerSeconds!.Value, Input.StudentList));
        if (!result.Succeeded)
        {
            var failure = MapFailure(result);
            if (failure is not null) return failure;
            ModelState.AddModelError(string.Empty, result.Error!);
            return Page();
        }

        // Chỉ gửi sau khi lưu thành công; thông điệp không chứa thông tin phiên thi/thí sinh.
        await live.NotifyAsync("Đã tạo phiên thi mới.", LiveTopics.Exams);
        FlashSuccess("Đã tạo phiên thi.");
        return RedirectToPage("./Details", new { id = result.Data });
    }

    private async Task LoadSubjectsAsync() => Subjects = await exams.GetSubjectsForExamAsync(CurrentUserId);
}
