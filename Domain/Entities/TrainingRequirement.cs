namespace TechnicalTrainingPlanner.Domain.Entities;

public class TrainingRequirement
{
    public int JobRoleId { get; set; }
    public JobRole JobRole { get; set; } = null!;

    public int TrainingId { get; set; }
    public Training Training { get; set; } = null!;
}
