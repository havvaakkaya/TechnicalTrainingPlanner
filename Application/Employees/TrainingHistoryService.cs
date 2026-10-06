using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TechnicalTrainingPlanner.Domain.Entities;
using TechnicalTrainingPlanner.Infrastructure.Persistence;
using TechnicalTrainingPlanner.ViewModels.Employees;

namespace TechnicalTrainingPlanner.Application.Employees;

public sealed class TrainingHistoryService
{
    private readonly AppDbContext _db;
    public TrainingHistoryService(AppDbContext db) => _db = db;

    public async Task<TrainingHistoryFormViewModel?> FormAsync(int employeeId,
        TrainingHistoryFormViewModel? posted = null)
    {
        var employee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == employeeId);
        if (employee is null) return null;
        var form = posted ?? new TrainingHistoryFormViewModel { CompletedOn = DateOnly.FromDateTime(DateTime.Today) };
        form.EmployeeId = employeeId;
        form.EmployeeLabel = employee.Code + " · " + employee.DisplayName;
        var trainings = await _db.Trainings.AsNoTracking().OrderBy(x => x.Code).ToListAsync();
        form.Trainings = trainings.Select(x => new SelectListItem(x.Code + " · " + x.Name, x.Id.ToString())).ToList();
        return form;
    }

    public async Task<List<(string Field, string Message)>> ValidateAsync(TrainingHistoryFormViewModel form)
    {
        var errors = new List<(string, string)>();
        if (!await _db.Employees.AnyAsync(x => x.Id == form.EmployeeId))
            errors.Add((nameof(form.EmployeeId), "Çalışan bulunamadı."));
        if (!await _db.Trainings.AnyAsync(x => x.Id == form.TrainingId))
            errors.Add((nameof(form.TrainingId), "Geçerli eğitim seçiniz."));
        if (form.CompletedOn is null)
            errors.Add((nameof(form.CompletedOn), "Tamamlama tarihi giriniz."));
        else if (await _db.EmployeeTrainings.AnyAsync(x => x.EmployeeId == form.EmployeeId &&
            x.TrainingId == form.TrainingId && x.CompletedOn == form.CompletedOn.Value && x.Passed == form.Passed))
            errors.Add((nameof(form.CompletedOn), "Aynı gün ve sonuç için bu geçmiş kaydı zaten var."));
        return errors;
    }

    public async Task SaveAsync(TrainingHistoryFormViewModel form)
    {
        _db.EmployeeTrainings.Add(new EmployeeTraining
        {
            EmployeeId = form.EmployeeId, TrainingId = form.TrainingId,
            CompletedOn = form.CompletedOn!.Value, Passed = form.Passed,
            EvidenceReference = form.EvidenceReference?.Trim()
        });
        await _db.SaveChangesAsync();
    }
}
