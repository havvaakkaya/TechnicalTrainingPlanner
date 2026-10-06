namespace TechnicalTrainingPlanner.Application.Planning;

public sealed class GreedyPlanner
{
    public PlanResult Solve(PlanningScenario scenario)
    {
        var sessions = scenario.SessionById;
        var chosen = new List<Candidate>();
        foreach (var need in scenario.Needs
            .OrderByDescending(x => PlanRules.Critical(x, scenario.Analysis))
            .ThenByDescending(x => x.PriorityScore).ThenBy(x => x.Id))
        {
            foreach (var pair in scenario.Candidates.Where(x => x.NeedId == need.Id)
                .OrderBy(x => sessions[x.SessionId].StartsAtUtc).ThenBy(x => x.SessionId))
            {
                chosen.Add(pair);
                try { PlanRules.Validate(scenario, chosen); break; }
                catch (InvalidOperationException) { chosen.RemoveAt(chosen.Count - 1); }
            }
        }
        PlanRules.Validate(scenario, chosen);
        return new PlanResult("GREEDY",
            scenario.Needs.Count > 0 && chosen.Count == 0 ? "NO_ASSIGNMENTS" : "FEASIBLE", chosen);
    }
}
