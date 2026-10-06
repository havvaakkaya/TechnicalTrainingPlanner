using Microsoft.AspNetCore.Mvc;
using TechnicalTrainingPlanner.Application.Planning;
using TechnicalTrainingPlanner.Application.Queries;

namespace TechnicalTrainingPlanner.Controllers;

public sealed class PlanningController : Controller
{
    private readonly ScreenQueryService _queries;
    private readonly PlanningService _planning;
    public PlanningController(ScreenQueryService queries, PlanningService planning)
    { _queries = queries; _planning = planning; }

    public async Task<IActionResult> Index(int? analysisId)
        => View(await _queries.PlanningAsync(analysisId));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Run(int analysisId)
    {
        try { return RedirectToAction("Index", "Reports", new { runId = await _planning.PlanAsync(analysisId) }); }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Index", await _queries.PlanningAsync(analysisId));
        }
    }
}
