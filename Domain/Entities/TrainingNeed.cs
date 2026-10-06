namespace TechnicalTrainingPlanner.Domain.Entities;

public class TrainingNeed
{
    public int Id { get; set; }

    public int NeedAnalysisRunId { get; set; }
    public NeedAnalysisRun NeedAnalysisRun { get; set; } = null!;

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int TrainingId { get; set; }
    public Training Training { get; set; } = null!;

    public DateOnly? DueOn { get; set; }
    public string NeedType { get; set; } = string.Empty;
    public int PriorityScore { get; set; }
}
