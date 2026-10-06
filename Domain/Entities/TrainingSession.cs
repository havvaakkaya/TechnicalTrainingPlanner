namespace TechnicalTrainingPlanner.Domain.Entities;

public class TrainingSession
{
    public int Id { get; set; }

    public int TrainingId { get; set; }
    public Training Training { get; set; } = null!;

    public int InstructorId { get; set; }
    public Instructor Instructor { get; set; } = null!;

    public int ClassroomId { get; set; }
    public Classroom Classroom { get; set; } = null!;

    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public int Capacity { get; set; }
    public string Status { get; set; } = "PLANNED";
}
