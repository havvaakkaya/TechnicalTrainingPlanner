using Microsoft.AspNetCore.Mvc.Rendering;

namespace TechnicalTrainingPlanner.ViewModels.Employees;

public class EmployeesIndexViewModel
{
    public int? DepartmentId { get; set; }
    public int? JobRoleId { get; set; }
    public string? StatusCode { get; set; }
    public List<SelectListItem> Departments { get; set; } = new();
    public List<SelectListItem> JobRoles { get; set; } = new();
    public List<SelectListItem> Statuses { get; set; } = new();
    public List<EmployeeRowViewModel> Employees { get; set; } = new();
}

public class EmployeeRowViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string JobRoleName { get; set; } = string.Empty;
    public string PersonnelStatuses { get; set; } = string.Empty;
}
