using Microsoft.AspNetCore.Mvc;
using TechnicalTrainingPlanner.Application.Catalog;
using TechnicalTrainingPlanner.ViewModels.Trainings;

namespace TechnicalTrainingPlanner.Controllers;

public class TrainingsController : Controller
{
    private readonly TrainingCatalogService _catalog;
    private readonly TrainingCatalogCommandService _commands;

    public TrainingsController(TrainingCatalogService catalog, TrainingCatalogCommandService commands)
    {
        _catalog = catalog;
        _commands = commands;
    }

    public async Task<IActionResult> Index(string? search)
        => View(await _catalog.GetIndexAsync(search));

    public async Task<IActionResult> Details(int id)
    {
        var model = await _catalog.GetDetailsAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new TrainingFormViewModel();
        await _commands.PopulateChoicesAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TrainingFormViewModel model)
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
    public async Task<IActionResult> Edit(int id, TrainingFormViewModel model)
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
