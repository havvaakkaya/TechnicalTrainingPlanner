namespace TechnicalTrainingPlanner.Domain.Entities;

public class TrainingPrerequisite
{
    public int TrainingId { get; set; }
    public Training Training { get; set; } = null!;

    public int PrerequisiteTrainingId { get; set; }
    public Training PrerequisiteTraining { get; set; } = null!;
}
