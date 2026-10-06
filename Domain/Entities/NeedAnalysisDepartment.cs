namespace TechnicalTrainingPlanner.Domain.Entities;

public class NeedAnalysisDepartment
{
    public int NeedAnalysisRunId { get; set; }
    public NeedAnalysisRun NeedAnalysisRun { get; set; } = null!;

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
}
