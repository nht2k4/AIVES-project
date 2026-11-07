// Sinh bởi Scaffold-DbContext (Database First). Sửa database rồi scaffold lại, không sửa tay file này.
using System;
using System.Collections.Generic;

namespace AIVES.DataAccess.Entities;

public partial class ExamSession
{
    public int Id { get; set; }

    public int SubjectId { get; set; }

    public string Title { get; set; } = null!;

    public DateTime StartTime { get; set; }

    public int SlotMinutes { get; set; }

    public int MainQuestionCount { get; set; }

    public int MaxFollowUps { get; set; }

    public int MaxFollowUpsPerQuestion { get; set; }

    public int AnswerSeconds { get; set; }

    public string Status { get; set; } = null!;

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<ExamParticipant> ExamParticipants { get; set; } = new List<ExamParticipant>();

    public virtual Subject Subject { get; set; } = null!;
}
