namespace TechnicalTrainingPlanner.Domain.Entities;

public class TrainingAssignment
{
    public int Id { get; set; }

    public int OptimizationRunId { get; set; }
    public OptimizationRun OptimizationRun { get; set; } = null!;

    public int TrainingNeedId { get; set; }
    public TrainingNeed TrainingNeed { get; set; } = null!;

    public int TrainingSessionId { get; set; }
    public TrainingSession TrainingSession { get; set; } = null!;
}
