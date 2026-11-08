using AIVES.Business.DTOs;

namespace AIVES.Ass3.MVC.ViewModels;

public record VoicesViewModel(VoiceOverviewDto Overview, IReadOnlyList<SubjectDto> Subjects, LanguageConfigDto? SelectedSubject);
