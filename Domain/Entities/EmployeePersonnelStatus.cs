namespace TechnicalTrainingPlanner.Domain.Entities;

public class EmployeePersonnelStatus
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public string PersonnelStatusCode { get; set; } = string.Empty;
    public PersonnelStatus PersonnelStatus { get; set; } = null!;
}
