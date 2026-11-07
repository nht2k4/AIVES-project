// Sinh bởi Scaffold-DbContext (Database First). Sửa database rồi scaffold lại, không sửa tay file này.
using System;
using System.Collections.Generic;

namespace AIVES.DataAccess.Entities;

public partial class ExamParticipant
{
    public int Id { get; set; }

    public int SessionId { get; set; }

    public string StudentCode { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public DateTime SlotStart { get; set; }

    public DateTime SlotEnd { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<InterviewTurn> InterviewTurns { get; set; } = new List<InterviewTurn>();

    public virtual ICollection<ParticipantQuestion> ParticipantQuestions { get; set; } = new List<ParticipantQuestion>();

    public virtual ExamSession Session { get; set; } = null!;
}
