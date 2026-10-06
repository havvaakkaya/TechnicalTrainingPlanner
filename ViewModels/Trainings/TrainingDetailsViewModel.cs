namespace TechnicalTrainingPlanner.ViewModels.Trainings;

public class TrainingDetailsViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int? RenewalMonths { get; set; }
    public string RuleBasis { get; set; } = string.Empty;
    public string? SourceReference { get; set; }
    public List<TrainingRelationViewModel> RequiredByRoles { get; set; } = new();
    public List<TrainingRelationViewModel> QualifiedInstructors { get; set; } = new();
    public List<TrainingRelationViewModel> AudienceStatuses { get; set; } = new();
    public List<TrainingRelationViewModel> Prerequisites { get; set; } = new();
}

public class TrainingRelationViewModel
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
