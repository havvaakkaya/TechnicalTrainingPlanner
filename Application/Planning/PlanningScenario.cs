using TechnicalTrainingPlanner.Domain.Entities;

namespace TechnicalTrainingPlanner.Application.Planning;

public sealed record Candidate(int NeedId, int SessionId, int EmployeeId, int DepartmentId);
public sealed record DutySlice(int DepartmentId, DateTime Start, DateTime End,
    int AvailableForTraining, int[] CandidateIndexes);
public sealed record PlanResult(string Method, string Status, List<Candidate> Assignments);

public sealed class PlanningScenario
{
    public required NeedAnalysisRun Analysis { get; init; }
    public required List<TrainingNeed> Needs { get; init; }
    public required List<TrainingSession> Sessions { get; init; }
    public required List<Candidate> Candidates { get; init; }
    public required List<DutySlice> DutySlices { get; init; }
    public required List<Employee> Employees { get; init; }
    public required List<EmployeeAvailability> Availability { get; init; }
    public required List<Department> Departments { get; init; }
    public required List<TrainingPrerequisite> Prerequisites { get; init; }
    public required List<EmployeeTraining> History { get; init; }
    public Dictionary<int, TrainingNeed> NeedById => Needs.ToDictionary(x => x.Id);
    public Dictionary<int, TrainingSession> SessionById => Sessions.ToDictionary(x => x.Id);
}
