using AIVES.Business.DTOs;
using AIVES.Business.Models;
using AIVES.DataAccess.Entities;

namespace AIVES.Business.Services;

// Database lưu Role/Language dạng chuỗi ("Admin", "vi-VN"). Business đổi sang enum có kiểu rõ ràng.
// Entity không bao giờ đi lên tầng Presentation.
internal static class Mapper
{
    public static AccountDto ToDto(this Account a) =>
        new(a.Id, a.FullName, a.Email, ParseRole(a.Role), a.IsActive, a.CreatedAt);

    public static SubjectDto ToDto(this Subject s) =>
        new(s.Id, s.Code, s.Name, s.IsActive,
            AppLanguageExtensions.FromLocaleCode(s.SttLanguage),
            AppLanguageExtensions.FromLocaleCode(s.TtsLanguage),
            s.LecturerSubjects.Count);

    public static LanguageConfigDto ToLanguageDto(this Subject s) =>
        new(s.Id, s.Code, s.Name,
            AppLanguageExtensions.FromLocaleCode(s.SttLanguage),
            AppLanguageExtensions.FromLocaleCode(s.TtsLanguage),
            s.LanguageUpdatedAt, s.LanguageUpdatedBy);

    public static AppRole ParseRole(string role) =>
        Enum.TryParse<AppRole>(role, out var parsed) ? parsed : AppRole.Lecturer;
}
