using System.ComponentModel.DataAnnotations;
using AIVES.Business.DTOs;
using AIVES.Business.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AIVES.Ass3.MVC.ViewModels;

// TV5 - Ass3 - việc 5.2

// Trang Chi tiết môn: dữ liệu môn + ô chọn giảng viên để phân công
public class SubjectDetailsViewModel
{
    [ValidateNever] public SubjectDetailDto Detail { get; set; } = null!;

    [Display(Name = "Giảng viên")]
    public int? LecturerId { get; set; }
}

// Trang Ngôn ngữ STT/TTS + khung Chỉnh tay cho phòng thi
public class LanguageEditViewModel
{
    [ValidateNever] public LanguageConfigDto Config { get; set; } = null!;

    [Display(Name = "Ngôn ngữ nhận dạng giọng nói (STT)")]
    public AppLanguage SttLanguage { get; set; }

    [Display(Name = "Ngôn ngữ đọc văn bản (TTS)")]
    public AppLanguage TtsLanguage { get; set; }

    // Mỗi dòng "cách nghe sai => cách viết đúng". Định dạng từng dòng do Service kiểm (báo đúng số dòng).
    [Display(Name = "Từ điển thuật ngữ")]
    [StringLength(2000, ErrorMessage = "Từ điển thuật ngữ không quá 2000 ký tự.")]
    public string? SttTerms { get; set; }

    [Display(Name = "Thời gian chờ AI (giây)")]
    [Range(2, 30, ErrorMessage = "Thời gian chờ AI từ 2 đến 30 giây.")]
    public int AiTimeoutSeconds { get; set; } = 8;

    [Display(Name = "Cho phép gửi câu trả lời tới AI bên ngoài")]
    public bool UseExternalAi { get; set; } = true;
}
