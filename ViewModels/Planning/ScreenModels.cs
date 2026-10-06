namespace TechnicalTrainingPlanner.ViewModels.Planning;

public sealed record DepartmentChoice(int Id, string Code, string Name);
public sealed record AnalysisChoice(int Id, DateOnly Start, DateOnly End, int NeedCount);
public sealed record NeedRow(int Id, int EmployeeId, string EmployeeCode, string EmployeeName,
    string Department, string TrainingCode, string TrainingName, DateOnly? DueOn,
    string Type, int Priority, bool Critical);
public sealed class NeedsPage
{
    public int? RunId { get; set; }
    public string Month { get; set; } = "2026-10";
    public string? Type { get; set; }
    public int? DepartmentId { get; set; }
    public bool IsCurrent { get; set; }
    public List<DepartmentChoice> Departments { get; set; } = new();
    public List<AnalysisChoice> Analyses { get; set; } = new();
    public List<NeedRow> Rows { get; set; } = new();
}
public sealed class NeedDetailsPage
{
    public required NeedRow Need { get; init; }
    public int RunId { get; init; }
    public string Role { get; init; } = "";
    public DateOnly? LastCompleted { get; init; }
    public int? RenewalMonths { get; init; }
    public string RuleBasis { get; init; } = "";
    public string? SourceReference { get; init; }
    public string[] AudienceStatuses { get; init; } = Array.Empty<string>();
}
public sealed class PlanningPage
{
    public int? AnalysisId { get; set; }
    public DateOnly? Start { get; set; }
    public DateOnly? End { get; set; }
    public int Needs { get; set; }
    public int Critical { get; set; }
    public int Sessions { get; set; }
    public bool Current { get; set; }
    public int? SelectedResultId { get; set; }
    public List<AnalysisChoice> Analyses { get; set; } = new();
}
public sealed record AssignmentRow(int NeedId, int EmployeeId, string Employee, string Department,
    string Training, int Priority, bool Critical, DateOnly? DueOn,
    DateTime StartsLocal, string Classroom, string Instructor);
public sealed record UnassignedRow(int NeedId, string Employee, string Department, string Training,
    int Priority, bool Critical, DateOnly? DueOn, string Reasons);
public sealed record MethodRow(int Id, string Method, string Status, int Assigned, int Critical,
    int PrioritySum, bool Selected, long Objective);
public sealed class ReportPage
{
    public int RunId { get; set; }
    public int AnalysisId { get; set; }
    public DateOnly Start { get; set; }
    public DateOnly End { get; set; }
    public string Method { get; set; } = "";
    public int Needs { get; set; }
    public int TotalNeeds { get; set; }
    public bool IsCurrent { get; set; }
    public int? DepartmentId { get; set; }
    public int? TrainingId { get; set; }
    public string? Status { get; set; }
    public List<DepartmentChoice> Departments { get; set; } = new();
    public List<(int Id, string Code, string Name)> Trainings { get; set; } = new();
    public List<AssignmentRow> Assignments { get; set; } = new();
    public List<UnassignedRow> Unassigned { get; set; } = new();
    public List<MethodRow> Methods { get; set; } = new();
}
public sealed record DepartmentCritical(string Code, string Name, int Employees);
public sealed record SessionNotice(int Id, string Training, DateTime StartsLocal, DateTime EndsLocal,
    string Instructor, string Classroom, string Location, int Capacity,
    int? Participants, int? TopPriority, string[] ParticipantNames);
public sealed class DashboardPage
{
    public int? AnalysisId { get; set; }
    public int? SelectedRunId { get; set; }
    public DateOnly? Start { get; set; }
    public int CriticalEmployees { get; set; }
    public int OverdueEmployees { get; set; }
    public int? UnassignedCriticalEmployees { get; set; }
    public int OverdueNeeds { get; set; }
    public int DueIn60Days { get; set; }
    public List<DepartmentCritical> Departments { get; set; } = new();
    public List<string> ShiftWarnings { get; set; } = new();
    public List<SessionNotice> Sessions { get; set; } = new();
}
public sealed record CalendarEntry(int SessionId, DateTime StartsLocal, DateTime EndsLocal,
    string Training, string Location, string Classroom, string Instructor,
    int Capacity, int? Participants);
public sealed class CalendarPage
{
    public int? AnalysisId { get; set; }
    public int? SelectedRunId { get; set; }
    public DateOnly? Month { get; set; }
    public List<CalendarEntry> Entries { get; set; } = new();
}
