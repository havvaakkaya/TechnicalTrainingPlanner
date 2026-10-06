using Microsoft.AspNetCore.Mvc.Rendering;

namespace TechnicalTrainingPlanner.ViewModels.Sessions;

public class SessionsIndexViewModel
{
    public int? TrainingId { get; set; }
    public string? Status { get; set; }
    public List<SelectListItem> Trainings { get; set; } = new();
    public List<SessionRowViewModel> Sessions { get; set; } = new();
}

public class SessionRowViewModel
{
    public int Id { get; set; }
    public string TrainingCode { get; set; } = string.Empty;
    public string TrainingName { get; set; } = string.Empty;
    public string InstructorCode { get; set; } = string.Empty;
    public string ClassroomCode { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public DateTime StartsAtLocal { get; set; }
    public DateTime EndsAtLocal { get; set; }
    public int Capacity { get; set; }
    public string Status { get; set; } = string.Empty;
}
