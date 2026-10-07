// Sinh bởi Scaffold-DbContext (Database First). Sửa database rồi scaffold lại, không sửa tay file này.
using System;
using System.Collections.Generic;

namespace AIVES.DataAccess.Entities;

public partial class Question
{
    public int Id { get; set; }

    public int SubjectId { get; set; }

    public string Content { get; set; } = null!;

    public string? ExpectedPoints { get; set; }

    public bool IsActive { get; set; }

    public virtual Subject Subject { get; set; } = null!;
}
