using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TechnicalTrainingPlanner.Application.Needs;
using TechnicalTrainingPlanner.Application.Planning;
using TechnicalTrainingPlanner.Application.Sessions;
using TechnicalTrainingPlanner.Domain.Entities;
using TechnicalTrainingPlanner.Infrastructure.Persistence;
using TechnicalTrainingPlanner.ViewModels.Planning;

namespace TechnicalTrainingPlanner.Application.Queries;

public sealed class ScreenQueryService
{
    private readonly AppDbContext _db;
    private readonly NeedAnalysisService _analysis;
    private readonly PlanningScenarioService _scenarios;
    public ScreenQueryService(AppDbContext db, NeedAnalysisService analysis, PlanningScenarioService scenarios)
    { _db = db; _analysis = analysis; _scenarios = scenarios; }

    public async Task<List<AnalysisChoice>> AnalysesAsync()
    {
        var runs = await _db.NeedAnalysisRuns.AsNoTracking()
            .OrderByDescending(x => x.Id).Take(30).ToListAsync();
        var ids = runs.Select(x => x.Id).ToArray();
        var counts = await _db.TrainingNeeds.AsNoTracking().Where(x => ids.Contains(x.NeedAnalysisRunId))
            .GroupBy(x => x.NeedAnalysisRunId)
            .Select(x => new { Id = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count);
        return runs.Select(x => new AnalysisChoice(x.Id, x.PlanningStart, x.PlanningEnd,
            counts.GetValueOrDefault(x.Id))).ToList();
    }

    public async Task<NeedsPage> NeedsAsync(int? runId, int? departmentId, string? type)
    {
        var analyses = await AnalysesAsync();
        runId ??= analyses.FirstOrDefault()?.Id;
        var model = new NeedsPage
        {
            RunId = runId, DepartmentId = departmentId, Type = type,
            Analyses = analyses,
            Departments = await _db.Departments.AsNoTracking().OrderBy(x => x.Code)
                .Select(x => new DepartmentChoice(x.Id, x.Code, x.Name)).ToListAsync()
        };
        if (runId is null) return model;
        var run = await _db.NeedAnalysisRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == runId);
        if (run is null) return model;
        model.Month = run.PlanningStart.ToString("yyyy-MM");
        model.IsCurrent = await _analysis.IsCurrentAsync(run);
        var query = _db.TrainingNeeds.AsNoTracking().Where(x => x.NeedAnalysisRunId == runId)
            .Include(x => x.Employee).ThenInclude(x => x.Department)
            .Include(x => x.Training).AsQueryable();
        if (departmentId.HasValue) query = query.Where(x => x.Employee.DepartmentId == departmentId.Value);
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(x => x.NeedType == type);
        model.Rows = (await query.OrderByDescending(x => x.PriorityScore).ThenBy(x => x.DueOn)
            .ThenBy(x => x.Employee.Code).ToListAsync())
            .Select(x => Row(x, run)).ToList();
        return model;
    }

    private static NeedRow Row(TrainingNeed n, NeedAnalysisRun run) =>
        new(n.Id, n.EmployeeId, n.Employee.Code, n.Employee.DisplayName,
            n.Employee.Department.Name, n.Training.Code, n.Training.Name, n.DueOn,
            n.NeedType, n.PriorityScore, PlanRules.Critical(n, run));

    public async Task<NeedDetailsPage?> NeedDetailsAsync(int id)
    {
        var need = await _db.TrainingNeeds.AsNoTracking().Include(x => x.NeedAnalysisRun)
            .Include(x => x.Training).Include(x => x.Employee).ThenInclude(x => x.Department)
            .Include(x => x.Employee).ThenInclude(x => x.JobRole)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (need is null) return null;
        var latest = await _db.EmployeeTrainings.AsNoTracking()
            .Where(x => x.EmployeeId == need.EmployeeId && x.TrainingId == need.TrainingId &&
                x.Passed && x.CompletedOn <= need.NeedAnalysisRun.PlanningStart)
            .OrderByDescending(x => x.CompletedOn).Select(x => (DateOnly?)x.CompletedOn).FirstOrDefaultAsync();
        var audience = await _db.TrainingAudienceStatuses.AsNoTracking()
            .Where(x => x.TrainingId == need.TrainingId)
            .Select(x => x.PersonnelStatus.Name).ToArrayAsync();
        return new NeedDetailsPage
        {
            Need = Row(need, need.NeedAnalysisRun), RunId = need.NeedAnalysisRunId,
            Role = need.Employee.JobRole.Name, LastCompleted = latest,
            RenewalMonths = need.Training.RenewalMonths, RuleBasis = need.Training.RuleBasis,
            SourceReference = need.Training.SourceReference, AudienceStatuses = audience
        };
    }

    public async Task<PlanningPage> PlanningAsync(int? analysisId)
    {
        var choices = await AnalysesAsync();
        analysisId ??= choices.FirstOrDefault()?.Id;
        var model = new PlanningPage { AnalysisId = analysisId, Analyses = choices };
        if (analysisId is null) return model;
        var run = await _db.NeedAnalysisRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == analysisId);
        if (run is null) return model;
        model.Start = run.PlanningStart; model.End = run.PlanningEnd;
        model.Current = await _analysis.IsCurrentAsync(run);
        model.Needs = await _db.TrainingNeeds.CountAsync(x => x.NeedAnalysisRunId == run.Id);
        model.Critical = await _db.TrainingNeeds.CountAsync(x => x.NeedAnalysisRunId == run.Id &&
            x.DueOn != null && x.DueOn <= run.PlanningEnd);
        DateTime from = PilotTimeZone.ToUtc(run.PlanningStart.ToDateTime(TimeOnly.MinValue));
        DateTime to = PilotTimeZone.ToUtc(run.PlanningEnd.AddDays(1).ToDateTime(TimeOnly.MinValue));
        model.Sessions = await _db.TrainingSessions.CountAsync(x => x.Status == "PLANNED" &&
            x.StartsAtUtc >= from && x.StartsAtUtc < to);
        model.SelectedResultId = await _db.OptimizationRuns.AsNoTracking()
            .Where(x => x.NeedAnalysisRunId == run.Id && x.IsSelectedResult)
            .Select(x => (int?)x.Id).FirstOrDefaultAsync();
        return model;
    }

    public async Task<ReportPage?> ReportAsync(int runId, int? departmentId = null,
        int? trainingId = null, string? status = null)
    {
        var run = await _db.OptimizationRuns.AsNoTracking()
            .Include(x => x.NeedAnalysisRun).FirstOrDefaultAsync(x => x.Id == runId);
        if (run is null) return null;
        var analysis = run.NeedAnalysisRun;
        var needs = await _db.TrainingNeeds.AsNoTracking()
            .Where(x => x.NeedAnalysisRunId == analysis.Id)
            .Include(x => x.Employee).ThenInclude(x => x.Department)
            .Include(x => x.Training).ToListAsync();
        var byId = needs.ToDictionary(x => x.Id);
        var assignments = await _db.TrainingAssignments.AsNoTracking()
            .Where(x => x.OptimizationRunId == runId)
            .Include(x => x.TrainingSession).ThenInclude(x => x.Classroom)
            .Include(x => x.TrainingSession).ThenInclude(x => x.Instructor).ToListAsync();
        var ids = assignments.Select(x => x.TrainingNeedId).ToHashSet();
        var reasons = await _db.UnassignedReasons.AsNoTracking()
            .Where(x => x.OptimizationRunId == runId).ToListAsync();
        var methodRuns = await _db.OptimizationRuns.AsNoTracking()
            .Where(x => x.NeedAnalysisRunId == analysis.Id).OrderBy(x => x.Id).ToListAsync();
        var methodIds = methodRuns.Select(x => x.Id).ToArray();
        var counts = await _db.TrainingAssignments.AsNoTracking()
            .Where(x => methodIds.Contains(x.OptimizationRunId)).ToListAsync();
        var filtered = needs.Where(x => (!departmentId.HasValue || x.Employee.DepartmentId == departmentId.Value) &&
            (!trainingId.HasValue || x.TrainingId == trainingId.Value)).ToArray();
        var filteredIds = filtered.Select(x => x.Id).ToHashSet();
        string? normalizedStatus = status is "ASSIGNED" or "UNASSIGNED" ? status : null;
        var model = new ReportPage
        {
            RunId = runId, AnalysisId = analysis.Id, Start = analysis.PlanningStart,
            End = analysis.PlanningEnd, Method = run.Method,
            Needs = normalizedStatus == "ASSIGNED" ? filtered.Count(x => ids.Contains(x.Id)) :
                    normalizedStatus == "UNASSIGNED" ? filtered.Count(x => !ids.Contains(x.Id)) : filtered.Length,
            TotalNeeds = needs.Count, DepartmentId = departmentId, TrainingId = trainingId,
            Status = normalizedStatus,
            Departments = await _db.Departments.AsNoTracking().OrderBy(x => x.Code)
                .Select(x => new DepartmentChoice(x.Id, x.Code, x.Name)).ToListAsync(),
            Trainings = (await _db.Trainings.AsNoTracking().OrderBy(x => x.Code)
                .Select(x => new { x.Id, x.Code, x.Name }).ToListAsync())
                .Select(x => (x.Id, x.Code, x.Name)).ToList(),
            IsCurrent = await _analysis.IsCurrentAsync(analysis),
            Assignments = assignments.Where(x => filteredIds.Contains(x.TrainingNeedId) &&
                normalizedStatus != "UNASSIGNED").Select(x =>
            {
                var n = byId[x.TrainingNeedId]; var s = x.TrainingSession;
                return new AssignmentRow(n.Id, n.EmployeeId, n.Employee.Code, n.Employee.Department.Name,
                    n.Training.Code, n.PriorityScore, PlanRules.Critical(n, analysis), n.DueOn,
                    PilotTimeZone.ToLocal(s.StartsAtUtc), s.Classroom.Code, s.Instructor.Code);
            }).OrderByDescending(x => x.Priority).ThenBy(x => x.StartsLocal).ToList(),
            Unassigned = filtered.Where(x => !ids.Contains(x.Id) &&
                normalizedStatus != "ASSIGNED").Select(x => new UnassignedRow(
                x.Id, x.Employee.Code, x.Employee.Department.Name, x.Training.Code,
                x.PriorityScore, PlanRules.Critical(x, analysis), x.DueOn,
                string.Join("; ", reasons.Where(r => r.TrainingNeedId == x.Id).Select(r => r.Details ?? r.ReasonCode))))
                .OrderByDescending(x => x.Priority).ToList()
        };
        model.Methods = methodRuns.Select(m =>
        {
            var own = counts.Where(x => x.OptimizationRunId == m.Id).ToArray();
            long objective = 0;
            if (!string.IsNullOrEmpty(m.SettingsJson))
            {
                using var doc = JsonDocument.Parse(m.SettingsJson);
                if (doc.RootElement.TryGetProperty("Objective", out var value) &&
                    value.ValueKind == JsonValueKind.Number)
                    objective = value.GetInt64();
            }
            return new MethodRow(m.Id, m.Method, m.SolveStatus, own.Length,
                own.Count(x => PlanRules.Critical(byId[x.TrainingNeedId], analysis)),
                m.TotalScore, m.IsSelectedResult, objective);
        }).ToList();
        return model;
    }

    public async Task<DashboardPage> DashboardAsync()
    {
        var analysis = await _db.NeedAnalysisRuns.AsNoTracking().OrderByDescending(x => x.Id).FirstOrDefaultAsync();
        var model = new DashboardPage();
        if (analysis is null) return model;
        model.AnalysisId = analysis.Id; model.Start = analysis.PlanningStart;
        var needs = await _db.TrainingNeeds.AsNoTracking().Where(x => x.NeedAnalysisRunId == analysis.Id)
            .Include(x => x.Employee).ThenInclude(x => x.Department).ToListAsync();
        var critical = needs.Where(x => PlanRules.Critical(x, analysis)).ToArray();
        model.CriticalEmployees = critical.Select(x => x.EmployeeId).Distinct().Count();
        model.OverdueEmployees = needs.Where(x => x.NeedType == "OVERDUE")
            .Select(x => x.EmployeeId).Distinct().Count();
        model.OverdueNeeds = needs.Count(x => x.NeedType == "OVERDUE");
        model.DueIn60Days = needs.Count(x => x.NeedType == "DUE_SOON");
        model.Departments = critical.GroupBy(x => x.Employee.DepartmentId)
            .Select(g => new DepartmentCritical(g.First().Employee.Department.Code,
                g.First().Employee.Department.Name, g.Select(x => x.EmployeeId).Distinct().Count()))
            .OrderByDescending(x => x.Employees).ToList();
        var selected = await _db.OptimizationRuns.AsNoTracking().Where(x => x.NeedAnalysisRunId == analysis.Id &&
            x.IsSelectedResult).FirstOrDefaultAsync();
        model.SelectedRunId = selected?.Id;
        var assignments = selected is null ? new List<TrainingAssignment>() : await _db.TrainingAssignments
            .AsNoTracking().Where(x => x.OptimizationRunId == selected.Id)
            .Include(x => x.TrainingNeed).ThenInclude(x => x.Employee).ToListAsync();
        if (selected is not null)
        {
            var assignedIds = assignments.Select(x => x.TrainingNeedId).ToHashSet();
            model.UnassignedCriticalEmployees = critical.Where(x => !assignedIds.Contains(x.Id))
                .Select(x => x.EmployeeId).Distinct().Count();
            if (await _analysis.IsCurrentAsync(analysis))
            {
                var scenario = await _scenarios.BuildAsync(analysis.Id);
                model.ShiftWarnings = scenario.DutySlices.Select(slice =>
                {
                    int used = assignments.Count(a => slice.CandidateIndexes.Any(i =>
                        scenario.Candidates[i].NeedId == a.TrainingNeedId &&
                        scenario.Candidates[i].SessionId == a.TrainingSessionId));
                    return new { Slice = slice, Remaining = slice.AvailableForTraining - used };
                }).Where(x => x.Remaining <= 1)
                  .GroupBy(x => new { x.Slice.DepartmentId, Day = DateOnly.FromDateTime(
                      PilotTimeZone.ToLocal(x.Slice.Start)) })
                  .Take(8).Select(g => $"{scenario.Departments.Single(x => x.Id == g.Key.DepartmentId).Name} · {g.Key.Day:dd.MM.yyyy} · kalan tampon {g.Min(x => x.Remaining)}")
                  .ToList();
            }
            else model.ShiftWarnings.Add("Temel veriler değişti; güncel vardiya görünümü için yeni analiz gerekir.");
        }
        DateTime from = PilotTimeZone.ToUtc(analysis.PlanningStart.ToDateTime(TimeOnly.MinValue));
        DateTime to = PilotTimeZone.ToUtc(analysis.PlanningEnd.AddDays(1).ToDateTime(TimeOnly.MinValue));
        var sessions = await _db.TrainingSessions.AsNoTracking()
            .Where(x => x.Status == "PLANNED" && x.StartsAtUtc >= from && x.StartsAtUtc < to)
            .Include(x => x.Training).Include(x => x.Instructor).Include(x => x.Classroom)
            .OrderBy(x => x.StartsAtUtc).ToListAsync();
        model.Sessions = sessions.Select(x =>
        {
            var people = assignments.Where(a => a.TrainingSessionId == x.Id).ToArray();
            return new SessionNotice(x.Id, x.Training.Code, PilotTimeZone.ToLocal(x.StartsAtUtc),
                PilotTimeZone.ToLocal(x.EndsAtUtc), x.Instructor.Code, x.Classroom.Code,
                x.Classroom.LocationCode, x.Capacity, selected is null ? null : people.Length,
                people.Length == 0 ? null : people.Max(a => a.TrainingNeed.PriorityScore),
                people.Select(a => a.TrainingNeed.Employee.Code + " · " + a.TrainingNeed.Employee.DisplayName).ToArray());
        }).OrderByDescending(x => x.TopPriority ?? -1).ThenBy(x => x.StartsLocal).ToList();
        return model;
    }

    public async Task<CalendarPage> CalendarAsync(int? analysisId)
    {
        var run = analysisId.HasValue ? await _db.NeedAnalysisRuns.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == analysisId.Value) :
            await _db.NeedAnalysisRuns.AsNoTracking().OrderByDescending(x => x.Id).FirstOrDefaultAsync();
        var model = new CalendarPage();
        if (run is null) return model;
        model.AnalysisId = run.Id; model.Month = run.PlanningStart;
        model.SelectedRunId = await _db.OptimizationRuns.AsNoTracking()
            .Where(x => x.NeedAnalysisRunId == run.Id && x.IsSelectedResult)
            .Select(x => (int?)x.Id).FirstOrDefaultAsync();
        var counts = model.SelectedRunId is null ? new Dictionary<int, int>() : await _db.TrainingAssignments
            .AsNoTracking().Where(x => x.OptimizationRunId == model.SelectedRunId)
            .GroupBy(x => x.TrainingSessionId).Select(x => new { Id = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);
        DateTime from = PilotTimeZone.ToUtc(run.PlanningStart.ToDateTime(TimeOnly.MinValue));
        DateTime to = PilotTimeZone.ToUtc(run.PlanningEnd.AddDays(1).ToDateTime(TimeOnly.MinValue));
        model.Entries = (await _db.TrainingSessions.AsNoTracking()
            .Where(x => x.Status == "PLANNED" && x.StartsAtUtc >= from && x.StartsAtUtc < to)
            .Include(x => x.Training).Include(x => x.Classroom).Include(x => x.Instructor)
            .OrderBy(x => x.StartsAtUtc).ToListAsync())
            .Select(x => new CalendarEntry(x.Id, PilotTimeZone.ToLocal(x.StartsAtUtc),
                PilotTimeZone.ToLocal(x.EndsAtUtc), x.Training.Code + " · " + x.Training.Name,
                x.Classroom.LocationCode, x.Classroom.Code, x.Instructor.Code,
                x.Capacity, model.SelectedRunId is null ? null : counts.GetValueOrDefault(x.Id)))
            .ToList();
        return model;
    }
}
