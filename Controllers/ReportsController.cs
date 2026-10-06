using Microsoft.AspNetCore.Mvc;
using TechnicalTrainingPlanner.Application.Queries;
using TechnicalTrainingPlanner.Infrastructure.Export;

namespace TechnicalTrainingPlanner.Controllers;

public sealed class ReportsController : Controller
{
    private readonly ScreenQueryService _queries;
    private readonly ExcelExportService _export;
    public ReportsController(ScreenQueryService queries, ExcelExportService export)
    { _queries = queries; _export = export; }

    public async Task<IActionResult> Index(int? runId, int? departmentId, int? trainingId, string? status)
    {
        if (runId is null)
        {
            var latest = await _queries.PlanningAsync(null);
            if (latest.SelectedResultId is null) return View("Index", (object?)null);
            runId = latest.SelectedResultId;
        }
        var model = await _queries.ReportAsync(runId.Value, departmentId, trainingId, status);
        return model is null ? NotFound() : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Export(int runId, int? departmentId, int? trainingId, string? status)
    {
        var model = await _queries.ReportAsync(runId, departmentId, trainingId, status);
        if (model is null) return NotFound();
        byte[] bytes = _export.Create(model);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"TeknikEgitim_Rapor_{model.Start:yyyy_MM}_{runId}.xlsx");
    }
}
