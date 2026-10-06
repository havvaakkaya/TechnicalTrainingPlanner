using Microsoft.EntityFrameworkCore;
using TechnicalTrainingPlanner.Domain.Entities;
using TechnicalTrainingPlanner.Infrastructure.Persistence;

namespace TechnicalTrainingPlanner.Application.Needs;

public sealed class NeedAnalysisService
{
    private readonly AppDbContext _db;
    private readonly ScenarioFingerprintService _fingerprints;
    public NeedAnalysisService(AppDbContext db, ScenarioFingerprintService fingerprints)
    { _db = db; _fingerprints = fingerprints; }

    public async Task<int> CalculateAsync(DateOnly monthStart, IReadOnlyCollection<int> selectedDepartments)
    {
        if (monthStart.Day != 1) throw new InvalidOperationException("Planlama ayının ilk gününü seçiniz.");
        var ids = selectedDepartments.Distinct().OrderBy(x => x).ToArray();
        if (ids.Length == 0) throw new InvalidOperationException("En az bir birim seçiniz.");
        int matching = await _db.Departments.CountAsync(x => ids.Contains(x.Id));
        if (matching != ids.Length) throw new InvalidOperationException("Geçersiz birim seçimi.");
        DateOnly monthEnd = monthStart.AddMonths(1).AddDays(-1);
        DateOnly horizon = monthStart.AddDays(60);
        string before = await _fingerprints.ComputeAsync(monthStart, monthEnd, ids);

        var employees = await _db.Employees.AsNoTracking()
            .Where(x => x.IsActive && ids.Contains(x.DepartmentId)).OrderBy(x => x.Id).ToListAsync();
        var requirements = await _db.TrainingRequirements.AsNoTracking().ToListAsync();
        var trainings = await _db.Trainings.AsNoTracking().ToDictionaryAsync(x => x.Id);
        var audiences = await _db.TrainingAudienceStatuses.AsNoTracking().ToListAsync();
        var statuses = await _db.EmployeePersonnelStatuses.AsNoTracking().ToListAsync();
        var histories = await _db.EmployeeTrainings.AsNoTracking()
            .Where(x => x.Passed && x.CompletedOn <= monthStart).ToListAsync();
        var requirementsByRole = requirements.GroupBy(x => x.JobRoleId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.TrainingId).ToArray());
        var audienceByTraining = audiences.GroupBy(x => x.TrainingId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.PersonnelStatusCode).ToHashSet());
        var statusesByEmployee = statuses.GroupBy(x => x.EmployeeId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.PersonnelStatusCode).ToHashSet());
        var latestSuccess = histories.GroupBy(x => (x.EmployeeId, x.TrainingId))
            .ToDictionary(x => x.Key, x => x.Max(y => y.CompletedOn));

        await using var tx = await _db.Database.BeginTransactionAsync();
        var run = new NeedAnalysisRun
        {
            PlanningStart = monthStart, PlanningEnd = monthEnd, LookAheadDays = 60,
            ScenarioFingerprint = before, CreatedAtUtc = DateTime.UtcNow
        };
        _db.NeedAnalysisRuns.Add(run);
        await _db.SaveChangesAsync();
        _db.NeedAnalysisDepartments.AddRange(ids.Select(id => new NeedAnalysisDepartment
        { NeedAnalysisRunId = run.Id, DepartmentId = id }));

        foreach (var employee in employees)
        {
            if (!requirementsByRole.TryGetValue(employee.JobRoleId, out var required)) continue;
            foreach (int trainingId in required)
            {
                if (audienceByTraining.TryGetValue(trainingId, out var requiredStatuses) &&
                    (!statusesByEmployee.TryGetValue(employee.Id, out var actualStatuses) ||
                     !actualStatuses.Overlaps(requiredStatuses))) continue;

                var training = trainings[trainingId];
                bool completed = latestSuccess.TryGetValue((employee.Id, trainingId), out DateOnly last);
                DateOnly? due = null;
                string type;
                if (!completed) type = "MISSING";
                else if (training.RenewalMonths is int months)
                {
                    due = last.AddMonths(months);
                    if (due.Value < monthStart) type = "OVERDUE";
                    else if (due.Value <= horizon) type = "DUE_SOON";
                    else continue;
                }
                else continue;

                _db.TrainingNeeds.Add(new TrainingNeed
                {
                    NeedAnalysisRunId = run.Id, EmployeeId = employee.Id,
                    TrainingId = trainingId, DueOn = due, NeedType = type,
                    PriorityScore = Priority(type, due, monthStart, monthEnd)
                });
            }
        }
        await _db.SaveChangesAsync();
        string after = await _fingerprints.ComputeAsync(monthStart, monthEnd, ids);
        if (after != before) throw new InvalidOperationException("Analiz sırasında temel veriler değişti. Yeniden deneyiniz.");
        await tx.CommitAsync();
        return run.Id;
    }

    // Pilot önceliği: kritik 3000+, sonraki 60 gündeki diğer ihtiyaç 2000+, eksik 1000.
    // Bu puan eğitim alma aciliyetidir; planlama toplam amaç puanı ayrıca hesaplanır.
    public static int Priority(string type, DateOnly? due, DateOnly start, DateOnly end)
    {
        if (type == "OVERDUE") return 3000 + Math.Min(365, start.DayNumber - due!.Value.DayNumber);
        if (type == "DUE_SOON" && due <= end) return 3000 + end.DayNumber - due!.Value.DayNumber;
        if (type == "DUE_SOON") return 2000 + 60 - (due!.Value.DayNumber - start.DayNumber);
        return 1000;
    }

    public async Task<bool> IsCurrentAsync(NeedAnalysisRun run)
    {
        var ids = await _db.NeedAnalysisDepartments.AsNoTracking()
            .Where(x => x.NeedAnalysisRunId == run.Id).Select(x => x.DepartmentId).ToArrayAsync();
        return await _fingerprints.ComputeAsync(run.PlanningStart, run.PlanningEnd, ids) ==
               run.ScenarioFingerprint;
    }
}
