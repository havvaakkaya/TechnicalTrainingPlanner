namespace TechnicalTrainingPlanner.Domain.Entities;

public class Department
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public int MinOnDuty { get; set; }
}
