using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using TechnicalTrainingPlanner.Application.Needs;
using TechnicalTrainingPlanner.Application.Queries;

namespace TechnicalTrainingPlanner.Controllers;

public sealed class NeedsController : Controller
{
    private readonly ScreenQueryService _queries;
    private readonly NeedAnalysisService _analysis;
    public NeedsController(ScreenQueryService queries, NeedAnalysisService analysis)
    { _queries = queries; _analysis = analysis; }

    public async Task<IActionResult> Index(int? runId, int? departmentId, string? type)
        => View(await _queries.NeedsAsync(runId, departmentId, type));

    public async Task<IActionResult> Details(int id)
    {
        var model = await _queries.NeedDetailsAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculate(string month, int[]? departmentIds)
    {
        if (!DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var first))
            ModelState.AddModelError("month", "Geçerli bir ay seçiniz.");
        if (departmentIds is null || departmentIds.Length == 0)
            ModelState.AddModelError("departmentIds", "En az bir birim seçiniz.");
        if (!ModelState.IsValid)
        {
            var model = await _queries.NeedsAsync(null, null, null);
            model.Month = month;
            return View("Index", model);
        }
        try { return RedirectToAction(nameof(Index), new { runId = await _analysis.CalculateAsync(first, departmentIds!) }); }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var model = await _queries.NeedsAsync(null, null, null);
            model.Month = month;
            return View("Index", model);
        }
    }
}
