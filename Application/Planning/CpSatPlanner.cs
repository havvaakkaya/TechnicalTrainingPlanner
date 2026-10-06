using Google.OrTools.Sat;

namespace TechnicalTrainingPlanner.Application.Planning;

public sealed class CpSatPlanner
{
    public PlanResult Solve(PlanningScenario scenario, int seconds = 10)
    {
        var model = new CpModel();
        var vars = scenario.Candidates.Select((_, i) => model.NewBoolVar($"a{i}")).ToArray();
        var sessions = scenario.SessionById;
        var needs = scenario.NeedById;

        foreach (var group in scenario.Candidates.Select((x, i) => (Pair: x, Index: i)).GroupBy(x => x.Pair.NeedId))
            model.Add(LinearExpr.Sum(group.Select(x => (IntVar)vars[x.Index]).ToArray()) <= 1);
        foreach (var group in scenario.Candidates.Select((x, i) => (Pair: x, Index: i)).GroupBy(x => x.Pair.SessionId))
            model.Add(LinearExpr.Sum(group.Select(x => (IntVar)vars[x.Index]).ToArray()) <= sessions[group.Key].Capacity);
        foreach (var group in scenario.Candidates.Select((x, i) => (Pair: x, Index: i)).GroupBy(x => x.Pair.EmployeeId))
        {
            var pairs = group.ToArray();
            for (int a = 0; a < pairs.Length; a++)
                for (int b = a + 1; b < pairs.Length; b++)
                    if (PlanningScenarioService.Overlaps(
                        sessions[pairs[a].Pair.SessionId].StartsAtUtc, sessions[pairs[a].Pair.SessionId].EndsAtUtc,
                        sessions[pairs[b].Pair.SessionId].StartsAtUtc, sessions[pairs[b].Pair.SessionId].EndsAtUtc))
                        model.Add(LinearExpr.Sum(new IntVar[] { vars[pairs[a].Index], vars[pairs[b].Index] }) <= 1);
        }
        foreach (var slice in scenario.DutySlices)
            model.Add(LinearExpr.Sum(slice.CandidateIndexes.Select(x => (IntVar)vars[x]).ToArray())
                      <= slice.AvailableForTraining);

        long criticalWeight = PlanRules.CriticalWeight(scenario);
        var weights = scenario.Candidates.Select((x, i) =>
            checked((PlanRules.Critical(needs[x.NeedId], scenario.Analysis) ? criticalWeight : 0L)
                + (long)needs[x.NeedId].PriorityScore * 100
                + PlanRules.BufferPreference(scenario, i))).ToArray();
        if (vars.Length > 0) model.Maximize(LinearExpr.WeightedSum(vars.Select(x => (IntVar)x).ToArray(), weights));
        var solver = new CpSolver { StringParameters = $"max_time_in_seconds:{seconds} num_search_workers:1 random_seed:2026" };
        var status = solver.Solve(model);
        if (status is not (CpSolverStatus.Optimal or CpSolverStatus.Feasible))
            return new PlanResult("CP_SAT", status.ToString().ToUpperInvariant(), new List<Candidate>());
        var selected = scenario.Candidates.Where((_, i) => solver.Value(vars[i]) == 1).ToList();
        return new PlanResult("CP_SAT", scenario.Needs.Count > 0 && selected.Count == 0 ?
            "NO_ASSIGNMENTS" : status.ToString().ToUpperInvariant(), selected);
    }
}
