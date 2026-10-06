using Microsoft.AspNetCore.Mvc;
using TechnicalTrainingPlanner.Application.Sessions;
using TechnicalTrainingPlanner.ViewModels.Sessions;

namespace TechnicalTrainingPlanner.Controllers;

public class SessionsController : Controller
{
    private readonly SessionQueryService _queries;
    private readonly SessionCommandService _commands;

    public SessionsController(SessionQueryService queries, SessionCommandService commands)
    {
        _queries = queries;
        _commands = commands;
    }

    public async Task<IActionResult> Index(int? trainingId, string? status)
        => View(await _queries.GetIndexAsync(trainingId, status));

    public async Task<IActionResult> Details(int id)
    {
        var model = await _queries.GetDetailsAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new SessionFormViewModel
        {
            StartsAtLocal = new DateTime(2026, 10, 30, 13, 0, 0),
            EndsAtLocal = new DateTime(2026, 10, 30, 15, 0, 0),
            Capacity = 8
        };
        await _commands.PopulateChoicesAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SessionFormViewModel model)
    {
        foreach (var error in await _commands.ValidateAsync(model))
            ModelState.AddModelError(error.Field, error.Message);
        if (!ModelState.IsValid)
        {
            await _commands.PopulateChoicesAsync(model);
            return View(model);
        }
        int id = await _commands.CreateAsync(model);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _commands.GetEditFormAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SessionFormViewModel model)
    {
        model.Id = id;
        foreach (var error in await _commands.ValidateAsync(model, id))
            ModelState.AddModelError(error.Field, error.Message);
        if (!ModelState.IsValid)
        {
            await _commands.PopulateChoicesAsync(model);
            return View(model);
        }
        if (!await _commands.UpdateAsync(id, model)) return NotFound();
        return RedirectToAction(nameof(Details), new { id });
    }
}
