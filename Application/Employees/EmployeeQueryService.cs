using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TechnicalTrainingPlanner.Infrastructure.Persistence;
using TechnicalTrainingPlanner.ViewModels.Employees;

namespace TechnicalTrainingPlanner.Application.Employees;

public class EmployeeQueryService
{
    private readonly AppDbContext _db;

    public EmployeeQueryService(AppDbContext db) => _db = db;

    public async Task<EmployeesIndexViewModel> GetIndexAsync(
        int? departmentId, int? jobRoleId, string? statusCode)
    {
        var query = _db.Employees.AsNoTracking().Where(x => x.IsActive);
        if (departmentId.HasValue)
            query = query.Where(x => x.DepartmentId == departmentId.Value);
        if (jobRoleId.HasValue)
            query = query.Where(x => x.JobRoleId == jobRoleId.Value);
        if (!string.IsNullOrWhiteSpace(statusCode))
            query = query.Where(x => _db.EmployeePersonnelStatuses
                .Any(s => s.EmployeeId == x.Id && s.PersonnelStatusCode == statusCode));

        var rows = await query.OrderBy(x => x.Code).Select(x => new EmployeeRowViewModel
        {
            Id = x.Id,
            Code = x.Code,
            DisplayName = x.DisplayName,
            DepartmentName = x.Department.Name,
            JobRoleName = x.JobRole.Name
        }).ToListAsync();

        var ids = rows.Select(x => x.Id).ToArray();
        var statusRows = await _db.EmployeePersonnelStatuses.AsNoTracking()
            .Where(x => ids.Contains(x.EmployeeId))
            .Select(x => new { x.EmployeeId, x.PersonnelStatus.Name })
            .ToListAsync();
        var statusesByEmployee = statusRows
            .GroupBy(x => x.EmployeeId)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.Name)));
        foreach (var row in rows)
            row.PersonnelStatuses = statusesByEmployee.GetValueOrDefault(row.Id) ?? "—";

        var departments = await _db.Departments.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name })
            .ToListAsync();
        var roles = await _db.JobRoles.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name })
            .ToListAsync();
        var statuses = await _db.PersonnelStatuses.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem { Value = x.Code, Text = x.Name })
            .ToListAsync();

        return new EmployeesIndexViewModel
        {
            DepartmentId = departmentId,
            JobRoleId = jobRoleId,
            StatusCode = statusCode,
            Departments = departments,
            JobRoles = roles,
            Statuses = statuses,
            Employees = rows
        };
    }

    public async Task<EmployeeDetailsViewModel?> GetDetailsAsync(int id)
    {
        var employee = await _db.Employees.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new EmployeeDetailsViewModel
            {
                Id = x.Id,
                Code = x.Code,
                DisplayName = x.DisplayName,
                DepartmentName = x.Department.Name,
                JobRoleName = x.JobRole.Name,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync();
        if (employee is null) return null;

        employee.PersonnelStatuses = await _db.EmployeePersonnelStatuses.AsNoTracking()
            .Where(x => x.EmployeeId == id)
            .OrderBy(x => x.PersonnelStatus.Name)
            .Select(x => x.PersonnelStatus.Name)
            .ToListAsync();
        employee.TrainingHistory = await _db.EmployeeTrainings.AsNoTracking()
            .Where(x => x.EmployeeId == id)
            .OrderByDescending(x => x.CompletedOn)
            .Select(x => new TrainingHistoryRowViewModel
            {
                TrainingCode = x.Training.Code,
                TrainingName = x.Training.Name,
                CompletedOn = x.CompletedOn,
                Passed = x.Passed
            })
            .ToListAsync();

        return employee;
    }
}
