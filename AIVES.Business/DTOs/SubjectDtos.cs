using AIVES.Business.Models;

namespace AIVES.Business.DTOs;

public record SubjectDto(
    int Id, string Code, string Name, bool IsActive,
    AppLanguage SttLanguage, AppLanguage TtsLanguage, int LecturerCount);

public record AssignedLecturerDto(int Id, string FullName, string Email, bool IsActive, DateTime AssignedAt);

public record LecturerOptionDto(int Id, string FullName, string Email);

public record SubjectDetailDto(
    SubjectDto Subject,
    IReadOnlyList<AssignedLecturerDto> AssignedLecturers,
    IReadOnlyList<LecturerOptionDto> AvailableLecturers);
