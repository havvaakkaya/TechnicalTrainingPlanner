using Microsoft.AspNetCore.Mvc;
using TechnicalTrainingPlanner.Application.Queries;

namespace TechnicalTrainingPlanner.Controllers;

public sealed class CalendarController : Controller
{
    private readonly ScreenQueryService _queries;
    public CalendarController(ScreenQueryService queries) => _queries = queries;
    public async Task<IActionResult> Index(int? analysisId)
        => View(await _queries.CalendarAsync(analysisId));
}
