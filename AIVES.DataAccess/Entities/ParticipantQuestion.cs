// Sinh bởi Scaffold-DbContext (Database First). Sửa database rồi scaffold lại, không sửa tay file này.
using System;
using System.Collections.Generic;

namespace AIVES.DataAccess.Entities;

public partial class ParticipantQuestion
{
    public int ParticipantId { get; set; }

    public int QuestionId { get; set; }

    public int OrderNo { get; set; }

    public virtual ExamParticipant Participant { get; set; } = null!;

    public virtual Question Question { get; set; } = null!;
}
