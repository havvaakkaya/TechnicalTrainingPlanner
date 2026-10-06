using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TechnicalTrainingPlanner.ViewModels.Trainings;

public class TrainingFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Eğitim kodu zorunludur.")]
    [StringLength(20, ErrorMessage = "Kod en fazla 20 karakter olabilir.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Eğitim adı zorunludur.")]
    [StringLength(150, ErrorMessage = "Ad en fazla 150 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [Range(1, 1440, ErrorMessage = "Süre 1 ile 1440 dakika arasında olmalıdır.")]
    public int DurationMinutes { get; set; }

    [Range(1, 120, ErrorMessage = "Tekrar aralığı 1 ile 120 ay arasında olmalıdır.")]
    public int? RenewalMonths { get; set; }

    public List<int> SelectedRoleIds { get; set; } = new();
    public List<int> SelectedInstructorIds { get; set; } = new();
    public List<int> SelectedPrerequisiteIds { get; set; } = new();
    public List<SelectListItem> Roles { get; set; } = new();
    public List<SelectListItem> Instructors { get; set; } = new();
    public List<SelectListItem> AvailablePrerequisites { get; set; } = new();
    public bool HasSessions { get; set; }
    public bool IsSourceBased => Code == "E01";
}
