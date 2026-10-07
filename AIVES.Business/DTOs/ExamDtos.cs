namespace AIVES.Business.DTOs;

public record QuestionDto(int Id, string Content, string? ExpectedPoints, bool IsActive);

public record AddQuestionRequest(int SubjectId, string Content, string? ExpectedPoints);

public record ExamListItemDto(
    int Id, string Title, string SubjectCode, string SubjectName,
    DateTime StartTime, int ParticipantCount, string Status);

// StudentList: mỗi dòng một sinh viên, dạng "MSSV; Họ tên"
public record CreateExamRequest(
    int SubjectId, string Title, DateTime StartTime, int SlotMinutes,
    int MainQuestionCount, int MaxFollowUps, int MaxFollowUpsPerQuestion, int AnswerSeconds,
    string StudentList);

public record ParticipantDto(
    int Id, string StudentCode, string FullName,
    DateTime SlotStart, DateTime SlotEnd, string Status,
    IReadOnlyList<string> Questions);

public record ExamDetailDto(
    int Id, int SubjectId, string SubjectCode, string SubjectName, string Title,
    DateTime StartTime, int SlotMinutes, int MainQuestionCount,
    int MaxFollowUps, int MaxFollowUpsPerQuestion, int AnswerSeconds, string Status,
    IReadOnlyList<ParticipantDto> Participants);
