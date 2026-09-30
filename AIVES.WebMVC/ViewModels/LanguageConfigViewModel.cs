using System.ComponentModel.DataAnnotations;
using AIVES.Business.Models;

namespace AIVES.WebMVC.ViewModels;

public class LanguageConfigViewModel
{
    public int SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;

    [Display(Name = "Ngôn ngữ nhận dạng giọng nói (STT)")]
    public AppLanguage SttLanguage { get; set; }

    [Display(Name = "Ngôn ngữ đọc văn bản (TTS)")]
    public AppLanguage TtsLanguage { get; set; }

    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
