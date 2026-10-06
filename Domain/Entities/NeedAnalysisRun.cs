namespace TechnicalTrainingPlanner.Domain.Entities;

public class NeedAnalysisRun
{
    public int Id { get; set; }
    public DateOnly PlanningStart { get; set; }
    public DateOnly PlanningEnd { get; set; }
    public int LookAheadDays { get; set; } = 60;
    public string ScenarioFingerprint { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
