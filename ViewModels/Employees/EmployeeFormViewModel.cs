using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TechnicalTrainingPlanner.ViewModels.Employees;

public class EmployeeFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Çalışan kodu zorunludur.")]
    [StringLength(20, ErrorMessage = "Kod en fazla 20 karakter olabilir.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Çalışan adı zorunludur.")]
    [StringLength(100, ErrorMessage = "Ad en fazla 100 karakter olabilir.")]
    public string DisplayName { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Birim seçiniz.")]
    public int DepartmentId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Görev rolü seçiniz.")]
    public int JobRoleId { get; set; }

    public bool IsActive { get; set; } = true;
    public List<string> SelectedStatusCodes { get; set; } = new();

    public List<SelectListItem> Departments { get; set; } = new();
    public List<SelectListItem> JobRoles { get; set; } = new();
    public List<SelectListItem> Statuses { get; set; } = new();
}
