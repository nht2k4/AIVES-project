// Sinh bởi Scaffold-DbContext (Database First). Sửa database rồi scaffold lại, không sửa tay file này.
using System;
using System.Collections.Generic;

namespace AIVES.DataAccess.Entities;

public partial class InterviewTurn
{
    public int Id { get; set; }

    public int ParticipantId { get; set; }

    public int QuestionId { get; set; }

    public string Kind { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? Reason { get; set; }

    public string? Answer { get; set; }

    public DateTime AskedAt { get; set; }

    public DateTime? AnsweredAt { get; set; }

    public bool TimedOut { get; set; }

    public virtual ExamParticipant Participant { get; set; } = null!;

    public virtual Question Question { get; set; } = null!;
}
