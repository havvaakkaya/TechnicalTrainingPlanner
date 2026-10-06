using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TechnicalTrainingPlanner.Infrastructure.Persistence;

namespace TechnicalTrainingPlanner.Application.Needs;

// Bütün plan girdilerinin sıralı gösterimi. Sürüm değişince eski analiz güncel sayılmaz.
public sealed class ScenarioFingerprintService
{
    private readonly AppDbContext _db;
    public ScenarioFingerprintService(AppDbContext db) => _db = db;

    public async Task<string> ComputeAsync(DateOnly start, DateOnly end, IReadOnlyCollection<int> departmentIds)
    {
        var snapshot = new
        {
            Version = "pilot-needs-v1;score-v1;planning-v1;Europe/Istanbul",
            Start = start, End = end, DepartmentsSelected = departmentIds.OrderBy(x => x).ToArray(),
            Departments = await _db.Departments.AsNoTracking().OrderBy(x => x.Id)
                .Select(x => new { x.Id, x.Code, x.Name, x.LocationCode, x.MinOnDuty }).ToListAsync(),
            Employees = await _db.Employees.AsNoTracking().OrderBy(x => x.Id)
                .Select(x => new { x.Id, x.Code, x.DisplayName, x.IsActive, x.DepartmentId, x.JobRoleId }).ToListAsync(),
            Statuses = await _db.EmployeePersonnelStatuses.AsNoTracking()
                .OrderBy(x => x.EmployeeId).ThenBy(x => x.PersonnelStatusCode)
                .Select(x => new { x.EmployeeId, x.PersonnelStatusCode }).ToListAsync(),
            Trainings = await _db.Trainings.AsNoTracking().OrderBy(x => x.Id)
                .Select(x => new { x.Id, x.Code, x.Name, x.DurationMinutes, x.RenewalMonths, x.RuleBasis }).ToListAsync(),
            Requirements = await _db.TrainingRequirements.AsNoTracking()
                .OrderBy(x => x.JobRoleId).ThenBy(x => x.TrainingId)
                .Select(x => new { x.JobRoleId, x.TrainingId }).ToListAsync(),
            Audiences = await _db.TrainingAudienceStatuses.AsNoTracking()
                .OrderBy(x => x.TrainingId).ThenBy(x => x.PersonnelStatusCode)
                .Select(x => new { x.TrainingId, x.PersonnelStatusCode }).ToListAsync(),
            Prerequisites = await _db.TrainingPrerequisites.AsNoTracking()
                .OrderBy(x => x.TrainingId).ThenBy(x => x.PrerequisiteTrainingId)
                .Select(x => new { x.TrainingId, x.PrerequisiteTrainingId }).ToListAsync(),
            History = await _db.EmployeeTrainings.AsNoTracking().OrderBy(x => x.Id)
                .Select(x => new { x.Id, x.EmployeeId, x.TrainingId, x.CompletedOn, x.Passed }).ToListAsync(),
            Shifts = await _db.EmployeeAvailabilities.AsNoTracking().OrderBy(x => x.Id)
                .Select(x => new { x.Id, x.EmployeeId, x.WindowType, x.StartsAtUtc, x.EndsAtUtc }).ToListAsync(),
            Sessions = await _db.TrainingSessions.AsNoTracking().OrderBy(x => x.Id)
                .Select(x => new { x.Id, x.TrainingId, x.InstructorId, x.ClassroomId,
                    x.StartsAtUtc, x.EndsAtUtc, x.Capacity, x.Status }).ToListAsync(),
            Instructors = await _db.Instructors.AsNoTracking().OrderBy(x => x.Id)
                .Select(x => new { x.Id, x.Code, x.LocationCode }).ToListAsync(),
            Qualifications = await _db.InstructorTrainings.AsNoTracking()
                .OrderBy(x => x.InstructorId).ThenBy(x => x.TrainingId)
                .Select(x => new { x.InstructorId, x.TrainingId }).ToListAsync(),
            Classrooms = await _db.Classrooms.AsNoTracking().OrderBy(x => x.Id)
                .Select(x => new { x.Id, x.Code, x.LocationCode, x.Capacity }).ToListAsync()
        };
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(snapshot)));
        return Convert.ToHexString(bytes);
    }
}
