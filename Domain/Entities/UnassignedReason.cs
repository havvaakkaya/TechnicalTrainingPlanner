namespace TechnicalTrainingPlanner.Domain.Entities;

public class UnassignedReason
{
    public int Id { get; set; }

    public int OptimizationRunId { get; set; }
    public OptimizationRun OptimizationRun { get; set; } = null!;

    public int TrainingNeedId { get; set; }
    public TrainingNeed TrainingNeed { get; set; } = null!;

    public string ReasonCode { get; set; } = string.Empty;
    public string? Details { get; set; }
}
