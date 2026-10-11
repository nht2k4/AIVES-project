using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using Microsoft.AspNetCore.Authorization;

namespace AIVES.Ass2.Razor.Pages.MySubjects;

[Authorize(Roles = Roles.Lecturer)]
public class IndexModel(ISubjectService subjects) : TV5PageModel
{
    public IReadOnlyList<SubjectDto> Subjects { get; private set; } = Array.Empty<SubjectDto>();
    public async Task OnGetAsync() => Subjects = await subjects.GetByLecturerAsync(CurrentUserId);
}
