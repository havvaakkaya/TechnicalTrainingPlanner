namespace TechnicalTrainingPlanner.Application.Planning;

public static class PlanRules
{
    public static bool Critical(TechnicalTrainingPlanner.Domain.Entities.TrainingNeed need,
        TechnicalTrainingPlanner.Domain.Entities.NeedAnalysisRun run) =>
        need.DueOn.HasValue && need.DueOn.Value <= run.PlanningEnd;

    public static long CriticalWeight(PlanningScenario scenario) =>
        checked(1L + scenario.Needs.Sum(x => (long)x.PriorityScore * 100 + 100));

    public static int BufferPreference(PlanningScenario scenario, int candidateIndex)
    {
        var caps = scenario.DutySlices.Where(x => x.CandidateIndexes.Contains(candidateIndex))
            .Select(x => x.AvailableForTraining).ToArray();
        return caps.Length == 0 ? 0 : Math.Min(99, caps.Min());
    }

    public static long Objective(PlanningScenario scenario, IReadOnlyCollection<Candidate> selection)
    {
        var byId = scenario.NeedById;
        long weight = CriticalWeight(scenario);
        long result = 0;
        foreach (var pair in selection)
        {
            int idx = scenario.Candidates.IndexOf(pair);
            result = checked(result + (Critical(byId[pair.NeedId], scenario.Analysis) ? weight : 0)
                + (long)byId[pair.NeedId].PriorityScore * 100
                + BufferPreference(scenario, idx));
        }
        return result;
    }

    public static void Validate(PlanningScenario scenario, IReadOnlyCollection<Candidate> selection)
    {
        var allowed = scenario.Candidates.ToHashSet();
        if (selection.Any(x => !allowed.Contains(x)))
            throw new InvalidOperationException("Sonuçta uygun aday olmayan bir atama var.");
        if (selection.GroupBy(x => x.NeedId).Any(x => x.Count() > 1))
            throw new InvalidOperationException("Bir ihtiyaç birden çok kez atandı.");
        var sessions = scenario.SessionById;
        foreach (var group in selection.GroupBy(x => x.SessionId))
            if (group.Count() > sessions[group.Key].Capacity)
                throw new InvalidOperationException("Oturum kapasitesi aşıldı.");
        foreach (var group in selection.GroupBy(x => x.EmployeeId))
        {
            var chosen = group.Select(x => sessions[x.SessionId]).OrderBy(x => x.StartsAtUtc).ToArray();
            for (int i = 1; i < chosen.Length; i++)
                if (chosen[i - 1].EndsAtUtc > chosen[i].StartsAtUtc)
                    throw new InvalidOperationException("Çalışanın oturumları çakışıyor.");
        }
        var indexes = selection.Select(x => scenario.Candidates.IndexOf(x)).ToHashSet();
        foreach (var slice in scenario.DutySlices)
            if (slice.CandidateIndexes.Count(indexes.Contains) > slice.AvailableForTraining)
                throw new InvalidOperationException("Vardiyadaki asgari operasyon personeli ihlal edildi.");
    }
}
