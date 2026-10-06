using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TechnicalTrainingPlanner.ViewModels.Employees;

public sealed class TrainingHistoryFormViewModel
{
    public int EmployeeId { get; set; }
    public string EmployeeLabel { get; set; } = "";
    [Range(1, int.MaxValue, ErrorMessage = "Eğitim seçiniz.")]
    public int TrainingId { get; set; }
    [Required(ErrorMessage = "Tamamlama tarihi giriniz.")]
    public DateOnly? CompletedOn { get; set; }
    public bool Passed { get; set; } = true;
    [StringLength(120)]
    public string? EvidenceReference { get; set; }
    public List<SelectListItem> Trainings { get; set; } = new();
}
