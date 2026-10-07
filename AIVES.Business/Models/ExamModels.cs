namespace AIVES.Business.Models;

// Giá trị lưu trong database (cột VARCHAR có CHECK constraint)
public static class ExamStatuses
{
    public const string Open = "Open";
    public const string Closed = "Closed";
}

public static class ParticipantStatuses
{
    public const string Scheduled = "Scheduled";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
}
