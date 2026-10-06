namespace TechnicalTrainingPlanner.Domain.Entities;

public class TrainingAudienceStatus
{
    public int TrainingId { get; set; }
    public Training Training { get; set; } = null!;

    public string PersonnelStatusCode { get; set; } = string.Empty;
    public PersonnelStatus PersonnelStatus { get; set; } = null!;
}
