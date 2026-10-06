using Microsoft.AspNetCore.Mvc;
using TechnicalTrainingPlanner.Application.Queries;

namespace TechnicalTrainingPlanner.Controllers;

public sealed class DashboardController : Controller
{
    private readonly ScreenQueryService _queries;
    public DashboardController(ScreenQueryService queries) => _queries = queries;
    public async Task<IActionResult> Index() => View(await _queries.DashboardAsync());
}
