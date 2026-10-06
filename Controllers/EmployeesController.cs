using Microsoft.AspNetCore.Mvc;
using TechnicalTrainingPlanner.Application.Employees;
using TechnicalTrainingPlanner.ViewModels.Employees;

namespace TechnicalTrainingPlanner.Controllers;

public class EmployeesController : Controller
{
    private readonly EmployeeQueryService _employees;
    private readonly EmployeeCommandService _commands;
    private readonly TrainingHistoryService _history;

    public EmployeesController(EmployeeQueryService employees, EmployeeCommandService commands,
        TrainingHistoryService history)
    {
        _employees = employees;
        _commands = commands;
        _history = history;
    }

    public async Task<IActionResult> Index(int? departmentId, int? jobRoleId, string? statusCode)
        => View(await _employees.GetIndexAsync(departmentId, jobRoleId, statusCode));

    public async Task<IActionResult> Details(int id)
    {
        var employee = await _employees.GetDetailsAsync(id);
        return employee is null ? NotFound() : View(employee);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new EmployeeFormViewModel();
        await _commands.PopulateChoicesAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeFormViewModel model)
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
    public async Task<IActionResult> Edit(int id, EmployeeFormViewModel model)
    {
        foreach (var error in await _commands.ValidateAsync(model, id))
            ModelState.AddModelError(error.Field, error.Message);
        model.Id = id;
        if (!ModelState.IsValid)
        {
            await _commands.PopulateChoicesAsync(model);
            return View(model);
        }

        if (!await _commands.UpdateAsync(id, model)) return NotFound();
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> AddTrainingHistory(int id)
    {
        var form = await _history.FormAsync(id);
        return form is null ? NotFound() : View(form);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTrainingHistory(int id, TrainingHistoryFormViewModel model)
    {
        model.EmployeeId = id;
        foreach (var error in await _history.ValidateAsync(model))
            ModelState.AddModelError(error.Field, error.Message);
        if (!ModelState.IsValid)
        {
            var form = await _history.FormAsync(id, model);
            return form is null ? NotFound() : View(form);
        }
        await _history.SaveAsync(model);
        return RedirectToAction(nameof(Details), new { id });
    }
}
