namespace TechnicalTrainingPlanner.ViewModels.Employees;

public class EmployeeDetailsViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string JobRoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> PersonnelStatuses { get; set; } = new();
    public List<TrainingHistoryRowViewModel> TrainingHistory { get; set; } = new();
}

public class TrainingHistoryRowViewModel
{
    public string TrainingCode { get; set; } = string.Empty;
    public string TrainingName { get; set; } = string.Empty;
    public DateOnly CompletedOn { get; set; }
    public bool Passed { get; set; }
}
