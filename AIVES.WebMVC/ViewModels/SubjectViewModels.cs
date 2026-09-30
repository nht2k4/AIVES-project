using System.ComponentModel.DataAnnotations;
using AIVES.Business.DTOs;

namespace AIVES.WebMVC.ViewModels;

public class SubjectDetailsViewModel
{
    public SubjectDetailDto Detail { get; set; } = null!;
    public AssignLecturerViewModel Assign { get; set; } = new();
}

public class AssignLecturerViewModel
{
    public int SubjectId { get; set; }

    [Required(ErrorMessage = "Chọn giảng viên cần phân công.")]
    [Display(Name = "Giảng viên")]
    public int? LecturerId { get; set; }
}
