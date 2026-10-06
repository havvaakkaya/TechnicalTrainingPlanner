namespace TechnicalTrainingPlanner.Domain.Entities;

public class EmployeeTraining
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int TrainingId { get; set; }
    public Training Training { get; set; } = null!;

    public DateOnly CompletedOn { get; set; }
    public bool Passed { get; set; }
    public string? EvidenceReference { get; set; }
}
