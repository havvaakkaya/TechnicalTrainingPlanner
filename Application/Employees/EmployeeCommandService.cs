using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TechnicalTrainingPlanner.Domain.Entities;
using TechnicalTrainingPlanner.Infrastructure.Persistence;
using TechnicalTrainingPlanner.ViewModels.Employees;

namespace TechnicalTrainingPlanner.Application.Employees;

public class EmployeeCommandService
{
    private readonly AppDbContext _db;

    public EmployeeCommandService(AppDbContext db) => _db = db;

    public async Task<EmployeeFormViewModel?> GetEditFormAsync(int id)
    {
        var employee = await _db.Employees.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new EmployeeFormViewModel
            {
                Id = x.Id, Code = x.Code, DisplayName = x.DisplayName,
                DepartmentId = x.DepartmentId, JobRoleId = x.JobRoleId,
                IsActive = x.IsActive
            }).FirstOrDefaultAsync();
        if (employee is null) return null;

        employee.SelectedStatusCodes = await _db.EmployeePersonnelStatuses.AsNoTracking()
            .Where(x => x.EmployeeId == id)
            .Select(x => x.PersonnelStatusCode)
            .ToListAsync();
        await PopulateChoicesAsync(employee);
        return employee;
    }

    public async Task PopulateChoicesAsync(EmployeeFormViewModel model)
    {
        var departments = await _db.Departments.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
        var roles = await _db.JobRoles.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
        var statuses = await _db.PersonnelStatuses.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
        model.Departments = departments.Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();
        model.JobRoles = roles.Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();
        model.Statuses = statuses.Select(x => new SelectListItem(x.Name, x.Code)).ToList();
    }

    public async Task<List<(string Field, string Message)>> ValidateAsync(
        EmployeeFormViewModel model, int? editingId = null)
    {
        model.Code = (model.Code ?? "").Trim().ToUpperInvariant();
        model.DisplayName = (model.DisplayName ?? "").Trim();
        model.SelectedStatusCodes = (model.SelectedStatusCodes ?? new List<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var errors = new List<(string Field, string Message)>();
        if (model.Code.Length == 0) errors.Add((nameof(model.Code), "Çalışan kodu zorunludur."));
        if (model.DisplayName.Length == 0) errors.Add((nameof(model.DisplayName), "Çalışan adı zorunludur."));
        if (await _db.Employees.AsNoTracking()
            .AnyAsync(x => x.Code == model.Code && (!editingId.HasValue || x.Id != editingId.Value)))
            errors.Add((nameof(model.Code), "Bu çalışan kodu zaten kullanılıyor."));
        if (!await _db.Departments.AnyAsync(x => x.Id == model.DepartmentId))
            errors.Add((nameof(model.DepartmentId), "Geçerli bir birim seçiniz."));
        if (!await _db.JobRoles.AnyAsync(x => x.Id == model.JobRoleId))
            errors.Add((nameof(model.JobRoleId), "Geçerli bir görev rolü seçiniz."));

        var validCodes = await _db.PersonnelStatuses.AsNoTracking()
            .Select(x => x.Code).ToListAsync();
        if (model.SelectedStatusCodes.Except(validCodes, StringComparer.Ordinal).Any())
            errors.Add((nameof(model.SelectedStatusCodes), "Geçersiz personel statüsü seçildi."));
        return errors;
    }

    public async Task<int> CreateAsync(EmployeeFormViewModel model)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var employee = new Employee
        {
            Code = model.Code, DisplayName = model.DisplayName,
            DepartmentId = model.DepartmentId, JobRoleId = model.JobRoleId,
            IsActive = model.IsActive
        };
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();
        foreach (string code in model.SelectedStatusCodes)
            _db.EmployeePersonnelStatuses.Add(new EmployeePersonnelStatus
            {
                EmployeeId = employee.Id, PersonnelStatusCode = code
            });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return employee.Id;
    }

    public async Task<bool> UpdateAsync(int id, EmployeeFormViewModel model)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(x => x.Id == id);
        if (employee is null) return false;

        await using var transaction = await _db.Database.BeginTransactionAsync();
        employee.Code = model.Code;
        employee.DisplayName = model.DisplayName;
        employee.DepartmentId = model.DepartmentId;
        employee.JobRoleId = model.JobRoleId;
        employee.IsActive = model.IsActive;

        var existing = await _db.EmployeePersonnelStatuses
            .Where(x => x.EmployeeId == id).ToListAsync();
        var selected = model.SelectedStatusCodes.ToHashSet(StringComparer.Ordinal);
        var current = existing.Select(x => x.PersonnelStatusCode).ToHashSet(StringComparer.Ordinal);
        _db.EmployeePersonnelStatuses.RemoveRange(
            existing.Where(x => !selected.Contains(x.PersonnelStatusCode)));
        foreach (string code in selected.Except(current))
            _db.EmployeePersonnelStatuses.Add(new EmployeePersonnelStatus
            {
                EmployeeId = id, PersonnelStatusCode = code
            });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }
}
