using Microsoft.EntityFrameworkCore;
using TechnicalTrainingPlanner.Infrastructure.Persistence;
using TechnicalTrainingPlanner.ViewModels.Trainings;

namespace TechnicalTrainingPlanner.Application.Catalog;

public class TrainingCatalogService
{
    private readonly AppDbContext _db;

    public TrainingCatalogService(AppDbContext db) => _db = db;

    public async Task<TrainingsIndexViewModel> GetIndexAsync(string? search)
    {
        string term = search?.Trim() ?? string.Empty;
        var query = _db.Trainings.AsNoTracking();
        if (term.Length > 0)
        {
            string pattern = $"%{term}%";
            query = query.Where(x => EF.Functions.ILike(x.Code, pattern)
                || EF.Functions.ILike(x.Name, pattern));
        }

        var items = await query.OrderBy(x => x.Code)
            .Select(x => new TrainingRowViewModel
            {
                Id = x.Id, Code = x.Code, Name = x.Name,
                RenewalMonths = x.RenewalMonths,
                RuleBasis = x.RuleBasis
            }).ToListAsync();
        return new TrainingsIndexViewModel { Search = term, Trainings = items };
    }

    public async Task<TrainingDetailsViewModel?> GetDetailsAsync(int id)
    {
        var model = await _db.Trainings.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new TrainingDetailsViewModel
            {
                Id = x.Id, Code = x.Code, Name = x.Name,
                DurationMinutes = x.DurationMinutes,
                RenewalMonths = x.RenewalMonths,
                RuleBasis = x.RuleBasis,
                SourceReference = x.SourceReference
            }).FirstOrDefaultAsync();
        if (model is null) return null;

        model.RequiredByRoles = await _db.TrainingRequirements.AsNoTracking()
            .Where(x => x.TrainingId == id)
            .OrderBy(x => x.JobRole.Code)
            .Select(x => new TrainingRelationViewModel
            {
                Code = x.JobRole.Code, Name = x.JobRole.Name
            }).ToListAsync();
        model.QualifiedInstructors = await _db.InstructorTrainings.AsNoTracking()
            .Where(x => x.TrainingId == id)
            .OrderBy(x => x.Instructor.Code)
            .Select(x => new TrainingRelationViewModel
            {
                Code = x.Instructor.Code, Name = x.Instructor.LocationCode
            }).ToListAsync();
        model.AudienceStatuses = await _db.TrainingAudienceStatuses.AsNoTracking()
            .Where(x => x.TrainingId == id)
            .OrderBy(x => x.PersonnelStatus.Code)
            .Select(x => new TrainingRelationViewModel
            {
                Code = x.PersonnelStatus.Code, Name = x.PersonnelStatus.Name
            }).ToListAsync();
        model.Prerequisites = await _db.TrainingPrerequisites.AsNoTracking()
            .Where(x => x.TrainingId == id)
            .OrderBy(x => x.PrerequisiteTraining.Code)
            .Select(x => new TrainingRelationViewModel
            {
                Code = x.PrerequisiteTraining.Code,
                Name = x.PrerequisiteTraining.Name
            }).ToListAsync();
        return model;
    }
}
