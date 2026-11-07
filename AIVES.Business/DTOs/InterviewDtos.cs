namespace AIVES.Business.DTOs;

// Trạng thái hiện tại của buổi phỏng vấn: trình duyệt dùng nó để đọc câu hỏi (TTS), đếm ngược và nhận dạng giọng nói (STT)
public record InterviewStateDto(
    int ParticipantId, string StudentCode, string StudentName, string SubjectCode,
    bool Finished, int? TurnId, string? Kind, string? Content,
    int MainIndex, int MainTotal, int FollowUpIndex, int FollowUpMax,
    int SecondsLeft, int AnswerSeconds, string SttLocale, string TtsLocale, int SessionId);

public record TurnDto(int Id, string Kind, string Content, string? Reason, string? Answer, bool TimedOut, DateTime AskedAt);

public record TranscriptDto(
    int ParticipantId, int SessionId, string SessionTitle, string SubjectCode,
    string StudentCode, string StudentName, string Status, IReadOnlyList<TurnDto> Turns);

public record MyInterviewDto(
    int ParticipantId, string SessionTitle, string SubjectCode, string SubjectName,
    DateTime SlotStart, DateTime SlotEnd, string Status, bool SessionOpen);
