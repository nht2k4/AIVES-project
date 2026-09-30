// Sinh bởi Scaffold-DbContext (Database First). Sửa database rồi scaffold lại, không sửa tay file này.
using System;
using System.Collections.Generic;

namespace AIVES.DataAccess.Entities;

public partial class LecturerSubject
{
    public int LecturerId { get; set; }

    public int SubjectId { get; set; }

    public DateTime AssignedAt { get; set; }

    public virtual Account Lecturer { get; set; } = null!;

    public virtual Subject Subject { get; set; } = null!;
}
