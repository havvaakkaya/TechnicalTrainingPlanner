namespace TechnicalTrainingPlanner.Domain.Entities;

public class Employee
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public int JobRoleId { get; set; }
    public JobRole JobRole { get; set; } = null!;
}
