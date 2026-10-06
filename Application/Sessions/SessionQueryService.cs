using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TechnicalTrainingPlanner.Infrastructure.Persistence;
using TechnicalTrainingPlanner.ViewModels.Sessions;

namespace TechnicalTrainingPlanner.Application.Sessions;

public class SessionQueryService
{
    private readonly AppDbContext _db;

    public SessionQueryService(AppDbContext db) => _db = db;

    public async Task<SessionsIndexViewModel> GetIndexAsync(int? trainingId, string? status)
    {
        var query = _db.TrainingSessions.AsNoTracking();
        if (trainingId.HasValue) query = query.Where(x => x.TrainingId == trainingId.Value);
        if (status is "PLANNED" or "CANCELLED") query = query.Where(x => x.Status == status);

        var records = await query.OrderBy(x => x.StartsAtUtc)
            .Select(x => new
            {
                x.Id, x.Training.Code, TrainingName = x.Training.Name,
                InstructorCode = x.Instructor.Code, ClassroomCode = x.Classroom.Code,
                x.Classroom.LocationCode, x.StartsAtUtc, x.EndsAtUtc,
                x.Capacity, x.Status
            }).ToListAsync();
        var trainings = await _db.Trainings.AsNoTracking().OrderBy(x => x.Code).ToListAsync();
        return new SessionsIndexViewModel
        {
            TrainingId = trainingId,
            Status = status,
            Trainings = trainings.Select(x => new SelectListItem($"{x.Code} · {x.Name}", x.Id.ToString())).ToList(),
            Sessions = records.Select(x => new SessionRowViewModel
            {
                Id = x.Id, TrainingCode = x.Code, TrainingName = x.TrainingName,
                InstructorCode = x.InstructorCode, ClassroomCode = x.ClassroomCode,
                LocationCode = x.LocationCode,
                StartsAtLocal = PilotTimeZone.ToLocal(x.StartsAtUtc),
                EndsAtLocal = PilotTimeZone.ToLocal(x.EndsAtUtc),
                Capacity = x.Capacity, Status = x.Status
            }).ToList()
        };
    }

    public async Task<SessionDetailsViewModel?> GetDetailsAsync(int id)
    {
        var record = await _db.TrainingSessions.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id, TrainingCode = x.Training.Code, TrainingName = x.Training.Name,
                InstructorCode = x.Instructor.Code, ClassroomCode = x.Classroom.Code,
                x.Classroom.LocationCode, x.StartsAtUtc, x.EndsAtUtc,
                x.Capacity, x.Status
            }).FirstOrDefaultAsync();
        if (record is null) return null;
        var model = new SessionDetailsViewModel
        {
            Id = record.Id, TrainingCode = record.TrainingCode,
            TrainingName = record.TrainingName, InstructorCode = record.InstructorCode,
            ClassroomCode = record.ClassroomCode, LocationCode = record.LocationCode,
            StartsAtLocal = PilotTimeZone.ToLocal(record.StartsAtUtc),
            EndsAtLocal = PilotTimeZone.ToLocal(record.EndsAtUtc),
            Capacity = record.Capacity, Status = record.Status
        };
        DateOnly localDay = DateOnly.FromDateTime(model.StartsAtLocal);
        var selectedRuns = await _db.OptimizationRuns.AsNoTracking()
            .Where(x => x.IsSelectedResult).Include(x => x.NeedAnalysisRun)
            .OrderByDescending(x => x.Id).ToListAsync();
        var selected = selectedRuns.FirstOrDefault(x => x.NeedAnalysisRun.PlanningStart <= localDay &&
            localDay <= x.NeedAnalysisRun.PlanningEnd);
        if (selected is not null)
        {
            model.SelectedRunId = selected.Id;
            model.Participants = await _db.TrainingAssignments.AsNoTracking()
                .Where(x => x.OptimizationRunId == selected.Id && x.TrainingSessionId == id)
                .OrderBy(x => x.TrainingNeed.Employee.Code)
                .Select(x => x.TrainingNeed.Employee.Code + " · " + x.TrainingNeed.Employee.DisplayName)
                .ToListAsync();
            model.ParticipantCount = model.Participants.Count;
        }
        return model;
    }
}
