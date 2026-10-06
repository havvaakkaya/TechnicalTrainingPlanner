namespace TechnicalTrainingPlanner.Domain.Entities;

public class Training
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int? RenewalMonths { get; set; }
    public string RuleBasis { get; set; } = string.Empty;
    public string? SourceReference { get; set; }
}
