namespace TechnicalTrainingPlanner.Domain.Entities;

public class Classroom
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public int Capacity { get; set; }
}
