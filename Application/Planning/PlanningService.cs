using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TechnicalTrainingPlanner.Application.Needs;
using TechnicalTrainingPlanner.Domain.Entities;
using TechnicalTrainingPlanner.Infrastructure.Persistence;

namespace TechnicalTrainingPlanner.Application.Planning;

public sealed class PlanningService
{
    private readonly AppDbContext _db;
    private readonly PlanningScenarioService _scenarios;
    private readonly NeedAnalysisService _analysis;
    private readonly GreedyPlanner _greedy;
    private readonly CpSatPlanner _cpSat;
    public PlanningService(AppDbContext db, PlanningScenarioService scenarios,
        NeedAnalysisService analysis, GreedyPlanner greedy, CpSatPlanner cpSat)
    { _db = db; _scenarios = scenarios; _analysis = analysis; _greedy = greedy; _cpSat = cpSat; }

    public async Task<int> PlanAsync(int analysisId)
    {
        if (await _db.OptimizationRuns.AnyAsync(x => x.NeedAnalysisRunId == analysisId))
            throw new InvalidOperationException("Bu analiz zaten planlandı. Yeni plan için yeni analiz oluşturunuz.");
        PlanningScenario scenario = await _scenarios.BuildAsync(analysisId);
        PlanResult greedy = _greedy.Solve(scenario);
        PlanResult cp;
        try { cp = _cpSat.Solve(scenario); }
        catch (Exception ex) when (ex is DllNotFoundException or TypeInitializationException)
        {
            throw new InvalidOperationException("OR-Tools çalıştırılamadı. Google.OrTools paketinin yüklü olduğunu ve projeyi yeniden derlediğinizi denetleyiniz.", ex);
        }
        PlanRules.Validate(scenario, greedy.Assignments);
        if (cp.Status is "OPTIMAL" or "FEASIBLE") PlanRules.Validate(scenario, cp.Assignments);
        bool cpWins = (cp.Status is "OPTIMAL" or "FEASIBLE") &&
                      PlanRules.Objective(scenario, cp.Assignments) > PlanRules.Objective(scenario, greedy.Assignments);
        var selection = cpWins ? cp : greedy;

        await using var tx = await _db.Database.BeginTransactionAsync();
        if (!await _analysis.IsCurrentAsync(scenario.Analysis))
            throw new InvalidOperationException("Plan hesaplanırken temel veriler değişti. Yeni analiz oluşturunuz.");
        if (await _db.OptimizationRuns.AnyAsync(x => x.NeedAnalysisRunId == analysisId))
            throw new InvalidOperationException("Bu analiz başka bir işlemde planlandı.");
        int selectedRunId = 0;
        foreach (var result in new[] { greedy, cp })
        {
            var record = new OptimizationRun
            {
                NeedAnalysisRunId = analysisId, Method = result.Method,
                SolveStatus = result.Status, ScenarioFingerprint = scenario.Analysis.ScenarioFingerprint,
                TotalScore = checked(result.Assignments.Sum(x => scenario.NeedById[x.NeedId].PriorityScore)),
                IsSelectedResult = result == selection,
                SettingsJson = JsonSerializer.Serialize(new
                {
                    PriorityVersion = "pilot-score-v1", CriticalWeight = PlanRules.CriticalWeight(scenario),
                    CpSatSeconds = 10, CpSatSeed = 2026, OperationBufferBiasMax = 99,
                    Objective = PlanRules.Objective(scenario, result.Assignments),
                    CriticalCount = result.Assignments.Count(x => PlanRules.Critical(scenario.NeedById[x.NeedId], scenario.Analysis))
                }), StartedAtUtc = DateTime.UtcNow, FinishedAtUtc = DateTime.UtcNow
            };
            _db.OptimizationRuns.Add(record);
            await _db.SaveChangesAsync();
            if (record.IsSelectedResult) selectedRunId = record.Id;
            _db.TrainingAssignments.AddRange(result.Assignments.Select(x => new TrainingAssignment
            { OptimizationRunId = record.Id, TrainingNeedId = x.NeedId, TrainingSessionId = x.SessionId }));
            var assigned = result.Assignments.Select(x => x.NeedId).ToHashSet();
            foreach (var need in scenario.Needs.Where(x => !assigned.Contains(x.Id)))
                foreach (var reason in Reasons(scenario, result.Assignments, need, result.Status))
                    _db.UnassignedReasons.Add(new UnassignedReason
                    { OptimizationRunId = record.Id, TrainingNeedId = need.Id, ReasonCode = reason.Code,
                      Details = reason.Details });
            await _db.SaveChangesAsync();
        }
        if (!await _analysis.IsCurrentAsync(scenario.Analysis))
            throw new InvalidOperationException("Sonuç kaydedilirken temel veriler değişti; işlem geri alındı.");
        await tx.CommitAsync();
        return selectedRunId;
    }

    private static List<(string Code, string Details)> Reasons(PlanningScenario s,
        List<Candidate> assigned, TrainingNeed need, string solveStatus)
    {
        var reasons = new List<(string, string)>();
        if (solveStatus is not ("OPTIMAL" or "FEASIBLE" or "NO_ASSIGNMENTS"))
        {
            reasons.Add(("SOLVER_STATUS", $"Çözücü bu yöntem için geçerli sonuç üretemedi: {solveStatus}."));
            return reasons;
        }
        var sameTraining = s.Sessions.Where(x => x.TrainingId == need.TrainingId).ToArray();
        if (sameTraining.Length == 0)
            reasons.Add(("NO_SESSION", "Seçili ayda bu eğitim için planlı oturum bulunmuyor."));
        var required = s.Prerequisites.Where(x => x.TrainingId == need.TrainingId)
            .Select(x => x.PrerequisiteTrainingId).ToArray();
        if (required.Length > 0 && sameTraining.Length > 0 &&
            !sameTraining.Any(session => required.All(pre => s.History.Any(h =>
                h.EmployeeId == need.EmployeeId && h.TrainingId == pre && h.Passed &&
                h.CompletedOn < DateOnly.FromDateTime(
                    TechnicalTrainingPlanner.Application.Sessions.PilotTimeZone.ToLocal(session.StartsAtUtc))))))
            reasons.Add(("PREREQUISITE", "Oturumdan önce başarıyla tamamlanmış ön koşul yok."));
        var possible = s.Candidates.Where(x => x.NeedId == need.Id).ToArray();
        if (sameTraining.Length > 0 && possible.Length == 0 && reasons.All(x => x.Item1 != "PREREQUISITE"))
            reasons.Add(("AVAILABILITY", "Çalışanın konumu, vardiyası veya uygunluk aralığı oturumla uyuşmuyor."));
        if (possible.Length > 0)
        {
            var full = possible.Where(x => assigned.Count(a => a.SessionId == x.SessionId) >=
                                               s.SessionById[x.SessionId].Capacity).ToArray();
            if (full.Length > 0) reasons.Add(("CAPACITY", "Uygun oturumlardan en az birinin kontenjanı dolu."));
            bool blockedByDuty = possible.Any(pair =>
            {
                int idx = s.Candidates.IndexOf(pair);
                return s.DutySlices.Where(slice => slice.CandidateIndexes.Contains(idx))
                    .Any(slice => assigned.Count(a => slice.CandidateIndexes.Contains(s.Candidates.IndexOf(a)))
                                  >= slice.AvailableForTraining);
            });
            if (blockedByDuty) reasons.Add(("DUTY_MINIMUM", "En az bir uygun oturumda birimin asgari görevli sayısı sınırı dolu."));
            if (possible.Any(x => assigned.Any(a => a.EmployeeId == x.EmployeeId &&
                PlanningScenarioService.Overlaps(s.SessionById[x.SessionId].StartsAtUtc,
                    s.SessionById[x.SessionId].EndsAtUtc,
                    s.SessionById[a.SessionId].StartsAtUtc, s.SessionById[a.SessionId].EndsAtUtc))))
                reasons.Add(("EMPLOYEE_CONFLICT", "Çalışan aynı saatte başka bir eğitime atanmış."));
            bool viable = possible.Any(pair =>
            {
                if (assigned.Count(a => a.SessionId == pair.SessionId) >= s.SessionById[pair.SessionId].Capacity)
                    return false;
                if (assigned.Any(a => a.EmployeeId == pair.EmployeeId &&
                    PlanningScenarioService.Overlaps(s.SessionById[pair.SessionId].StartsAtUtc,
                        s.SessionById[pair.SessionId].EndsAtUtc,
                        s.SessionById[a.SessionId].StartsAtUtc, s.SessionById[a.SessionId].EndsAtUtc)))
                    return false;
                int idx = s.Candidates.IndexOf(pair);
                return !s.DutySlices.Where(slice => slice.CandidateIndexes.Contains(idx))
                    .Any(slice => assigned.Count(a => slice.CandidateIndexes.Contains(s.Candidates.IndexOf(a)))
                                  >= slice.AvailableForTraining);
            });
            if (viable) reasons.Add(("PRIORITY", "Geçerli oturum vardı; bu yöntemde diğer ihtiyaçlar tercih edildi."));
        }
        if (reasons.Count == 0) reasons.Add(("NO_ELIGIBLE_PAIR", "Geçerli bir çalışan–oturum eşleşmesi üretilemedi."));
        return reasons;
    }
}
