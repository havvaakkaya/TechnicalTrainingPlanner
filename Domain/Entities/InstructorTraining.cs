namespace TechnicalTrainingPlanner.Domain.Entities;

public class InstructorTraining
{
    public int InstructorId { get; set; }
    public Instructor Instructor { get; set; } = null!;

    public int TrainingId { get; set; }
    public Training Training { get; set; } = null!;
}
