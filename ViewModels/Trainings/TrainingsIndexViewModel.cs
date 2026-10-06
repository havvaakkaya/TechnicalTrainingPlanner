namespace TechnicalTrainingPlanner.ViewModels.Trainings;

public class TrainingsIndexViewModel
{
    public string Search { get; set; } = string.Empty;
    public List<TrainingRowViewModel> Trainings { get; set; } = new();
}

public class TrainingRowViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? RenewalMonths { get; set; }
    public string RuleBasis { get; set; } = string.Empty;
}
