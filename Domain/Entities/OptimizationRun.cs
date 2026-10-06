namespace TechnicalTrainingPlanner.Domain.Entities;

public class OptimizationRun
{
    public int Id { get; set; }

    public int NeedAnalysisRunId { get; set; }
    public NeedAnalysisRun NeedAnalysisRun { get; set; } = null!;

    public string Method { get; set; } = string.Empty;
    public string SolveStatus { get; set; } = string.Empty;
    public string ScenarioFingerprint { get; set; } = string.Empty;
    public int TotalScore { get; set; }
    public bool IsSelectedResult { get; set; }
    public string? SettingsJson { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAtUtc { get; set; }
}
