// Sinh bởi Scaffold-DbContext (Database First). Sửa database rồi scaffold lại, không sửa tay file này.
using System;
using System.Collections.Generic;

namespace AIVES.DataAccess.Entities;

public partial class TurnRecording
{
    public int TurnId { get; set; }

    public string ContentType { get; set; } = null!;

    public byte[] Audio { get; set; } = null!;

    public int SizeBytes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual InterviewTurn Turn { get; set; } = null!;
}
