namespace TechnicalTrainingPlanner.ViewModels.Sessions;

public class SessionDetailsViewModel
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
    public int? SelectedRunId { get; set; }
    public int? ParticipantCount { get; set; }
    public List<string> Participants { get; set; } = new();
}
