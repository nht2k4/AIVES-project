using System.ComponentModel.DataAnnotations;

namespace AIVES.Business.Models;

public enum AppLanguage
{
    [Display(Name = "Tiếng Việt (vi-VN)")] Vietnamese = 1,
    [Display(Name = "English (en-US)")] English = 2
}

public static class AppLanguageExtensions
{
    // Mã locale được lưu trong database và cũng là mã engine STT/TTS cần
    public static string ToLocaleCode(this AppLanguage language) => language switch
    {
        AppLanguage.English => "en-US",
        _ => "vi-VN"
    };

    public static AppLanguage FromLocaleCode(string? code) =>
        code == "en-US" ? AppLanguage.English : AppLanguage.Vietnamese;
}
