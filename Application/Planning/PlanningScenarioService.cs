using Microsoft.EntityFrameworkCore;
using TechnicalTrainingPlanner.Application.Needs;
using TechnicalTrainingPlanner.Application.Sessions;
using TechnicalTrainingPlanner.Domain.Entities;
using TechnicalTrainingPlanner.Infrastructure.Persistence;

namespace TechnicalTrainingPlanner.Application.Planning;

public sealed class PlanningScenarioService
{
    private readonly AppDbContext _db;
    private readonly NeedAnalysisService _analysis;
    public PlanningScenarioService(AppDbContext db, NeedAnalysisService analysis)
    { _db = db; _analysis = analysis; }

    public async Task<PlanningScenario> BuildAsync(int analysisId)
    {
        var run = await _db.NeedAnalysisRuns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == analysisId)
            ?? throw new InvalidOperationException("Analiz bulunamadı.");
        if (!await _analysis.IsCurrentAsync(run))
            throw new InvalidOperationException("Analizden sonra temel veriler değişti. Yeni ihtiyaç analizi oluşturunuz.");
        int[] ids = await _db.NeedAnalysisDepartments.AsNoTracking()
            .Where(x => x.NeedAnalysisRunId == run.Id).Select(x => x.DepartmentId).ToArrayAsync();
        DateTime fromUtc = PilotTimeZone.ToUtc(run.PlanningStart.ToDateTime(TimeOnly.MinValue));
        DateTime toUtc = PilotTimeZone.ToUtc(run.PlanningEnd.AddDays(1).ToDateTime(TimeOnly.MinValue));
        var needs = await _db.TrainingNeeds.AsNoTracking().Where(x => x.NeedAnalysisRunId == run.Id)
            .OrderBy(x => x.Id).ToListAsync();
        var sessions = await _db.TrainingSessions.AsNoTracking()
            .Where(x => x.Status == "PLANNED" && x.StartsAtUtc >= fromUtc && x.StartsAtUtc < toUtc)
            .OrderBy(x => x.StartsAtUtc).ThenBy(x => x.Id).ToListAsync();
        var employees = await _db.Employees.AsNoTracking()
            .Where(x => x.IsActive && ids.Contains(x.DepartmentId)).ToListAsync();
        var departments = await _db.Departments.AsNoTracking()
            .Where(x => ids.Contains(x.Id)).ToListAsync();
        int[] empIds = employees.Select(x => x.Id).ToArray();
        var availability = await _db.EmployeeAvailabilities.AsNoTracking()
            .Where(x => empIds.Contains(x.EmployeeId) && x.StartsAtUtc < toUtc && x.EndsAtUtc > fromUtc)
            .ToListAsync();
        var history = await _db.EmployeeTrainings.AsNoTracking()
            .Where(x => empIds.Contains(x.EmployeeId) && x.Passed).ToListAsync();
        var prerequisites = await _db.TrainingPrerequisites.AsNoTracking().ToListAsync();
        var trainings = await _db.Trainings.AsNoTracking().ToDictionaryAsync(x => x.Id);
        var instructors = await _db.Instructors.AsNoTracking().ToDictionaryAsync(x => x.Id);
        var classrooms = await _db.Classrooms.AsNoTracking().ToDictionaryAsync(x => x.Id);
        var qualifications = (await _db.InstructorTrainings.AsNoTracking().ToListAsync())
            .Select(x => (x.InstructorId, x.TrainingId)).ToHashSet();

        foreach (var session in sessions)
        {
            if (!trainings.TryGetValue(session.TrainingId, out var training) ||
                !instructors.TryGetValue(session.InstructorId, out var teacher) ||
                !classrooms.TryGetValue(session.ClassroomId, out var room) ||
                session.Capacity < 1 || session.Capacity > room.Capacity ||
                teacher.LocationCode != room.LocationCode ||
                !qualifications.Contains((session.InstructorId, session.TrainingId)) ||
                session.EndsAtUtc <= session.StartsAtUtc ||
                (session.EndsAtUtc - session.StartsAtUtc).TotalMinutes != training.DurationMinutes)
                throw new InvalidOperationException($"{session.Id} numaralı oturumun eğitmen, sınıf, süre veya kontenjan bilgisi geçersiz.");
        }
        for (int i = 0; i < sessions.Count; i++)
            for (int j = i + 1; j < sessions.Count; j++)
                if (Overlaps(sessions[i].StartsAtUtc, sessions[i].EndsAtUtc,
                             sessions[j].StartsAtUtc, sessions[j].EndsAtUtc) &&
                    (sessions[i].InstructorId == sessions[j].InstructorId ||
                     sessions[i].ClassroomId == sessions[j].ClassroomId))
                    throw new InvalidOperationException($"{sessions[i].Id} ve {sessions[j].Id} numaralı oturumlar eğitmen veya sınıfta çakışıyor.");

        var employeesById = employees.ToDictionary(x => x.Id);
        var roomById = classrooms;
        var shiftsByEmployee = availability.GroupBy(x => x.EmployeeId)
            .ToDictionary(x => x.Key, x => x.ToArray());
        var preByTraining = prerequisites.GroupBy(x => x.TrainingId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.PrerequisiteTrainingId).ToArray());
        var historyByPair = history.GroupBy(x => (x.EmployeeId, x.TrainingId))
            .ToDictionary(x => x.Key, x => x.ToArray());
        var candidates = new List<Candidate>();
        foreach (var need in needs)
        {
            if (!employeesById.TryGetValue(need.EmployeeId, out var employee)) continue;
            var dept = departments.Single(x => x.Id == employee.DepartmentId);
            foreach (var session in sessions.Where(x => x.TrainingId == need.TrainingId))
            {
                if (dept.LocationCode != roomById[session.ClassroomId].LocationCode) continue;
                if (!IsWorking(need.EmployeeId, session.StartsAtUtc, session.EndsAtUtc, shiftsByEmployee)) continue;
                DateOnly day = DateOnly.FromDateTime(PilotTimeZone.ToLocal(session.StartsAtUtc));
                if (preByTraining.TryGetValue(need.TrainingId, out var required) &&
                    required.Any(pre => !historyByPair.TryGetValue((need.EmployeeId, pre), out var entries) ||
                                        !entries.Any(x => x.CompletedOn < day))) continue;
                candidates.Add(new Candidate(need.Id, session.Id, need.EmployeeId, employee.DepartmentId));
            }
        }

        var slices = new List<DutySlice>();
        foreach (var dept in departments)
        {
            var members = employees.Where(x => x.DepartmentId == dept.Id).ToArray();
            var deptCandidates = candidates.Select((x, i) => (Pair: x, Index: i))
                .Where(x => x.Pair.DepartmentId == dept.Id).ToArray();
            // Birden fazla oturumun üst üste geldiği en küçük aralıklar; izin ve vardiya sınırları dahil.
            var periods = sessions.Where(x => roomById[x.ClassroomId].LocationCode == dept.LocationCode).ToArray();
            var boundaries = periods.SelectMany(x => new[] { x.StartsAtUtc, x.EndsAtUtc })
                .Concat(availability.Where(x => members.Any(e => e.Id == x.EmployeeId))
                    .SelectMany(x => new[] { x.StartsAtUtc, x.EndsAtUtc }))
                .Distinct().OrderBy(x => x).ToArray();
            for (int k = 0; k + 1 < boundaries.Length; k++)
            {
                DateTime a = boundaries[k], b = boundaries[k + 1];
                if (!periods.Any(x => Overlaps(a, b, x.StartsAtUtc, x.EndsAtUtc))) continue;
                int baseCount = members.Count(x => IsWorking(x.Id, a, b, shiftsByEmployee));
                if (baseCount < dept.MinOnDuty)
                    throw new InvalidOperationException($"{dept.Code} biriminde {PilotTimeZone.ToLocal(a):dd.MM HH:mm} vardiyasında başlangıç çalışan sayısı asgarinin altında ({baseCount} < {dept.MinOnDuty}).");
                int[] relevant = deptCandidates.Where(x =>
                {
                    var session = sessions.Single(s => s.Id == x.Pair.SessionId);
                    return Overlaps(a, b, session.StartsAtUtc, session.EndsAtUtc);
                }).Select(x => x.Index).ToArray();
                if (relevant.Length > 0)
                    slices.Add(new DutySlice(dept.Id, a, b, baseCount - dept.MinOnDuty, relevant));
            }
        }
        return new PlanningScenario
        {
            Analysis = run, Needs = needs, Sessions = sessions, Employees = employees,
            Availability = availability, Departments = departments, Prerequisites = prerequisites,
            History = history, Candidates = candidates, DutySlices = slices
        };
    }

    public static bool Overlaps(DateTime a, DateTime b, DateTime c, DateTime d) => a < d && c < b;

    private static bool IsWorking(int employeeId, DateTime start, DateTime end,
        Dictionary<int, EmployeeAvailability[]> shifts)
    {
        if (!shifts.TryGetValue(employeeId, out var windows)) return false;
        return windows.Any(x => x.WindowType == "WORKING_SHIFT" &&
                                x.StartsAtUtc <= start && x.EndsAtUtc >= end) &&
               !windows.Any(x => x.WindowType == "UNAVAILABLE" &&
                                 Overlaps(start, end, x.StartsAtUtc, x.EndsAtUtc));
    }
}
