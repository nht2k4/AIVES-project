// Sinh bởi Scaffold-DbContext (Database First). Sửa database rồi scaffold lại, không sửa tay file này.
using System;
using System.Collections.Generic;

namespace AIVES.DataAccess.Entities;

public partial class Subject
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }

    public string SttLanguage { get; set; } = null!;

    public string TtsLanguage { get; set; } = null!;

    public DateTime? LanguageUpdatedAt { get; set; }

    public string? LanguageUpdatedBy { get; set; }

    public virtual ICollection<LecturerSubject> LecturerSubjects { get; set; } = new List<LecturerSubject>();
}
