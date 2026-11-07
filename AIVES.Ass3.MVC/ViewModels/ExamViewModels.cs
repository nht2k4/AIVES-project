using System.ComponentModel.DataAnnotations;
using AIVES.Business.DTOs;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AIVES.Ass3.MVC.ViewModels;

public class ExamListViewModel
{
    public IReadOnlyList<ExamListItemDto> Exams { get; set; } = [];
}

public class ExamCreateViewModel
{
    [Required(ErrorMessage = "Chọn môn học.")]
    [Display(Name = "Môn học")]
    public int SubjectId { get; set; }

    [Required(ErrorMessage = "Nhập tiêu đề phiên thi.")]
    [StringLength(150, ErrorMessage = "Tiêu đề không quá 150 ký tự.")]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Chọn thời gian bắt đầu.")]
    [Display(Name = "Thời gian bắt đầu")]
    public DateTime StartTime { get; set; }

    [Range(1, 120, ErrorMessage = "Khung thời gian từ 1 đến 120 phút.")]
    [Display(Name = "Khung thời gian (phút/thí sinh)")]
    public int SlotMinutes { get; set; } = 10;

    [Range(1, 20, ErrorMessage = "Số câu hỏi chính từ 1 đến 20.")]
    [Display(Name = "Số câu hỏi chính mỗi thí sinh")]
    public int MainQuestionCount { get; set; } = 2;

    [Range(0, 40, ErrorMessage = "Số câu hỏi xoáy tối đa từ 0 đến 40.")]
    [Display(Name = "Số câu hỏi xoáy tối đa cả bài")]
    public int MaxFollowUps { get; set; } = 2;

    [Range(0, 5, ErrorMessage = "Số câu hỏi xoáy mỗi câu chính từ 0 đến 5.")]
    [Display(Name = "Số câu hỏi xoáy mỗi câu chính")]
    public int MaxFollowUpsPerQuestion { get; set; } = 1;

    [Range(10, 600, ErrorMessage = "Thời gian trả lời từ 10 đến 600 giây.")]
    [Display(Name = "Thời gian trả lời mỗi câu (giây)")]
    public int AnswerSeconds { get; set; } = 120;

    [Required(ErrorMessage = "Nhập danh sách thí sinh.")]
    [Display(Name = "Danh sách thí sinh")]
    public string StudentList { get; set; } = string.Empty;

    [ValidateNever]
    public IReadOnlyList<SubjectDto> Subjects { get; set; } = [];

    public string MinimumStartTime => DateTime.Now.ToString("yyyy-MM-ddTHH:mm");
}

public class ExamDetailViewModel
{
    public ExamDetailDto Exam { get; set; } = null!;

    public bool CanReallocate =>
        Exam.Status == "Open" && Exam.Participants.All(p => p.Status == "Scheduled");
}

public class ExamQuestionsViewModel
{
    public SubjectDto Subject { get; set; } = null!;
    public IReadOnlyList<QuestionDto> Questions { get; set; } = [];
    public bool IsAdmin { get; set; }
    public ExamAddQuestionInput NewQuestion { get; set; } = new();
}

public class ExamAddQuestionInput
{
    [Required(ErrorMessage = "Nhập nội dung câu hỏi.")]
    [StringLength(1000, ErrorMessage = "Nội dung câu hỏi không quá 1000 ký tự.")]
    [Display(Name = "Nội dung câu hỏi")]
    public string Content { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Ý chính cần trả lời không quá 1000 ký tự.")]
    [Display(Name = "Ý chính cần trả lời (ngăn cách bởi dấu ;)")]
    public string? ExpectedPoints { get; set; }
}
