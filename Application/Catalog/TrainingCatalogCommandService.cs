using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using TechnicalTrainingPlanner.Domain.Entities;
using TechnicalTrainingPlanner.Infrastructure.Persistence;
using TechnicalTrainingPlanner.ViewModels.Trainings;

namespace TechnicalTrainingPlanner.Application.Catalog;

public class TrainingCatalogCommandService
{
    private readonly AppDbContext _db;

    public TrainingCatalogCommandService(AppDbContext db) => _db = db;

    public async Task PopulateChoicesAsync(TrainingFormViewModel model)
    {
        var roles = await _db.JobRoles.AsNoTracking().OrderBy(x => x.Code).ToListAsync();
        var instructors = await _db.Instructors.AsNoTracking().OrderBy(x => x.Code).ToListAsync();
        var trainings = await _db.Trainings.AsNoTracking()
            .Where(x => x.Id != model.Id).OrderBy(x => x.Code).ToListAsync();
        model.Roles = roles.Select(x => new SelectListItem($"{x.Code} · {x.Name}", x.Id.ToString())).ToList();
        model.Instructors = instructors
            .Select(x => new SelectListItem($"{x.Code} · Konum {x.LocationCode}", x.Id.ToString()))
            .ToList();
        model.AvailablePrerequisites = trainings
            .Select(x => new SelectListItem($"{x.Code} · {x.Name}", x.Id.ToString())).ToList();
        model.HasSessions = model.Id != 0 &&
            await _db.TrainingSessions.AsNoTracking().AnyAsync(x => x.TrainingId == model.Id);
    }

    public async Task<TrainingFormViewModel?> GetEditFormAsync(int id)
    {
        var model = await _db.Trainings.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new TrainingFormViewModel
            {
                Id = x.Id, Code = x.Code, Name = x.Name,
                DurationMinutes = x.DurationMinutes, RenewalMonths = x.RenewalMonths
            }).FirstOrDefaultAsync();
        if (model is null) return null;
        model.SelectedRoleIds = await _db.TrainingRequirements.AsNoTracking()
            .Where(x => x.TrainingId == id).Select(x => x.JobRoleId).ToListAsync();
        model.SelectedInstructorIds = await _db.InstructorTrainings.AsNoTracking()
            .Where(x => x.TrainingId == id).Select(x => x.InstructorId).ToListAsync();
        model.SelectedPrerequisiteIds = await _db.TrainingPrerequisites.AsNoTracking()
            .Where(x => x.TrainingId == id).Select(x => x.PrerequisiteTrainingId).ToListAsync();
        await PopulateChoicesAsync(model);
        return model;
    }

    public async Task<List<(string Field, string Message)>> ValidateAsync(
        TrainingFormViewModel model, int? editingId = null)
    {
        model.Code = (model.Code ?? "").Trim().ToUpperInvariant();
        model.Name = (model.Name ?? "").Trim();
        model.SelectedRoleIds = (model.SelectedRoleIds ?? new List<int>()).Distinct().ToList();
        model.SelectedInstructorIds = (model.SelectedInstructorIds ?? new List<int>()).Distinct().ToList();
        model.SelectedPrerequisiteIds = (model.SelectedPrerequisiteIds ?? new List<int>()).Distinct().ToList();
        var errors = new List<(string Field, string Message)>();

        if (model.Code.Length == 0) errors.Add((nameof(model.Code), "Eğitim kodu zorunludur."));
        else if (!Regex.IsMatch(model.Code, @"^E[0-9]{2,}$"))
            errors.Add((nameof(model.Code), "Eğitim kodu E16 gibi E ve en az iki rakamdan oluşmalıdır."));
        if (model.Name.Length == 0) errors.Add((nameof(model.Name), "Eğitim adı zorunludur."));
        if (model.RenewalMonths.HasValue && model.RenewalMonths.Value <= 0)
            errors.Add((nameof(model.RenewalMonths), "Tekrar aralığı pozitif olmalıdır."));
        if (!editingId.HasValue && model.Code == "E01")
            errors.Add((nameof(model.Code), "E01 kaynaklı kural için ayrılmıştır."));
        if (await _db.Trainings.AsNoTracking().AnyAsync(x => x.Code == model.Code &&
                (!editingId.HasValue || x.Id != editingId.Value)))
            errors.Add((nameof(model.Code), "Bu eğitim kodu zaten kullanılıyor."));

        if (editingId.HasValue)
        {
            var existing = await _db.Trainings.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == editingId.Value);
            if (existing is null)
                errors.Add((string.Empty, "Eğitim bulunamadı."));
            else
            {
                if (model.Code != existing.Code)
                    errors.Add((nameof(model.Code), "Var olan eğitimin kodu değiştirilemez."));
                if (existing.Code == "E01" && model.RenewalMonths != 24)
                    errors.Add((nameof(model.RenewalMonths), "E01'in kaynaklı 24 aylık kuralı değiştirilemez."));
                if (existing.Code == "E01" && model.SelectedPrerequisiteIds.Count > 0)
                    errors.Add((nameof(model.SelectedPrerequisiteIds), "E01 için ön koşul tanımlanamaz."));
                if (model.DurationMinutes != existing.DurationMinutes &&
                    await _db.TrainingSessions.AnyAsync(x => x.TrainingId == editingId.Value))
                    errors.Add((nameof(model.DurationMinutes), "Oturumu olan eğitimin süresi değiştirilemez."));
            }
        }

        var roleIds = (await _db.JobRoles.AsNoTracking().Select(x => x.Id).ToListAsync()).ToHashSet();
        if (model.SelectedRoleIds.Count == 0)
            errors.Add((nameof(model.SelectedRoleIds), "En az bir görev rolü seçiniz."));
        if (model.SelectedRoleIds.Any(x => !roleIds.Contains(x)))
            errors.Add((nameof(model.SelectedRoleIds), "Geçersiz görev rolü seçildi."));

        var instructorIds = (await _db.Instructors.AsNoTracking().Select(x => x.Id).ToListAsync()).ToHashSet();
        if (model.SelectedInstructorIds.Count == 0)
            errors.Add((nameof(model.SelectedInstructorIds), "En az bir yetkili eğitmen seçiniz."));
        if (model.SelectedInstructorIds.Any(x => !instructorIds.Contains(x)))
            errors.Add((nameof(model.SelectedInstructorIds), "Geçersiz eğitmen seçildi."));
        if (editingId.HasValue)
        {
            var scheduledInstructorIds = await _db.TrainingSessions.AsNoTracking()
                .Where(x => x.TrainingId == editingId.Value)
                .Select(x => x.InstructorId).Distinct().ToListAsync();
            if (scheduledInstructorIds.Any(x => !model.SelectedInstructorIds.Contains(x)))
                errors.Add((nameof(model.SelectedInstructorIds),
                    "Bu eğitimin mevcut oturumunda görevli eğitmenin yetkisi kaldırılamaz."));
        }

        var trainingIds = (await _db.Trainings.AsNoTracking().Select(x => x.Id).ToListAsync()).ToHashSet();
        if (model.SelectedPrerequisiteIds.Any(x => !trainingIds.Contains(x)))
            errors.Add((nameof(model.SelectedPrerequisiteIds), "Geçersiz ön koşul eğitimi seçildi."));
        if (editingId.HasValue && model.SelectedPrerequisiteIds.Contains(editingId.Value))
            errors.Add((nameof(model.SelectedPrerequisiteIds), "Eğitim kendisinin ön koşulu olamaz."));

        if (model.SelectedRoleIds.Count > 0 && model.SelectedPrerequisiteIds.Count > 0)
        {
            var requiredPairs = await _db.TrainingRequirements.AsNoTracking()
                .Where(x => model.SelectedRoleIds.Contains(x.JobRoleId) &&
                            model.SelectedPrerequisiteIds.Contains(x.TrainingId))
                .Select(x => new { x.JobRoleId, x.TrainingId }).ToListAsync();
            if (requiredPairs.Count != model.SelectedRoleIds.Count * model.SelectedPrerequisiteIds.Count)
                errors.Add((nameof(model.SelectedPrerequisiteIds),
                    "Ön koşul eğitimi, seçilen her görev rolü için de gerekli olmalıdır."));
        }

        if (editingId.HasValue)
        {
            var dependentRoleIds = await (
                from prerequisite in _db.TrainingPrerequisites.AsNoTracking()
                join requirement in _db.TrainingRequirements.AsNoTracking()
                    on prerequisite.TrainingId equals requirement.TrainingId
                where prerequisite.PrerequisiteTrainingId == editingId.Value
                select requirement.JobRoleId).Distinct().ToListAsync();
            if (dependentRoleIds.Any(x => !model.SelectedRoleIds.Contains(x)))
                errors.Add((nameof(model.SelectedRoleIds),
                    "Bu eğitimi ön koşul olarak kullanan eğitimlerin görev rolleri kaldırılamaz."));

            var edges = await _db.TrainingPrerequisites.AsNoTracking()
                .Where(x => x.TrainingId != editingId.Value)
                .Select(x => new { x.TrainingId, x.PrerequisiteTrainingId })
                .ToListAsync();
            var graph = edges.GroupBy(x => x.TrainingId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.PrerequisiteTrainingId).ToArray());
            if (model.SelectedPrerequisiteIds.Any(id => HasPath(graph, id, editingId.Value)))
                errors.Add((nameof(model.SelectedPrerequisiteIds), "Seçim ön koşul döngüsü oluşturuyor."));
        }
        return errors;
    }

    private static bool HasPath(Dictionary<int, int[]> graph, int start, int target)
    {
        var seen = new HashSet<int>();
        var pending = new Stack<int>();
        pending.Push(start);
        while (pending.Count > 0)
        {
            int node = pending.Pop();
            if (node == target) return true;
            if (!seen.Add(node)) continue;
            if (graph.TryGetValue(node, out var next))
                foreach (int child in next) pending.Push(child);
        }
        return false;
    }

    public async Task<int> CreateAsync(TrainingFormViewModel model)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var training = new Training
        {
            Code = model.Code, Name = model.Name, DurationMinutes = model.DurationMinutes,
            RenewalMonths = model.RenewalMonths, RuleBasis = "PILOT"
        };
        _db.Trainings.Add(training);
        await _db.SaveChangesAsync();
        foreach (int roleId in model.SelectedRoleIds)
            _db.TrainingRequirements.Add(new TrainingRequirement
            {
                TrainingId = training.Id, JobRoleId = roleId
            });
        foreach (int instructorId in model.SelectedInstructorIds)
            _db.InstructorTrainings.Add(new InstructorTraining
            {
                TrainingId = training.Id, InstructorId = instructorId
            });
        foreach (int prerequisiteId in model.SelectedPrerequisiteIds)
            _db.TrainingPrerequisites.Add(new TrainingPrerequisite
            {
                TrainingId = training.Id, PrerequisiteTrainingId = prerequisiteId
            });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return training.Id;
    }

    public async Task<bool> UpdateAsync(int id, TrainingFormViewModel model)
    {
        var training = await _db.Trainings.FirstOrDefaultAsync(x => x.Id == id);
        if (training is null) return false;
        await using var transaction = await _db.Database.BeginTransactionAsync();
        training.Name = model.Name;
        training.DurationMinutes = model.DurationMinutes;
        if (training.Code != "E01") training.RenewalMonths = model.RenewalMonths;

        var roles = await _db.TrainingRequirements.Where(x => x.TrainingId == id).ToListAsync();
        var selectedRoles = model.SelectedRoleIds.ToHashSet();
        var currentRoles = roles.Select(x => x.JobRoleId).ToHashSet();
        _db.TrainingRequirements.RemoveRange(roles.Where(x => !selectedRoles.Contains(x.JobRoleId)));
        foreach (int roleId in selectedRoles.Except(currentRoles))
            _db.TrainingRequirements.Add(new TrainingRequirement
            {
                TrainingId = id, JobRoleId = roleId
            });

        var instructorLinks = await _db.InstructorTrainings
            .Where(x => x.TrainingId == id).ToListAsync();
        var selectedInstructors = model.SelectedInstructorIds.ToHashSet();
        var currentInstructors = instructorLinks.Select(x => x.InstructorId).ToHashSet();
        _db.InstructorTrainings.RemoveRange(
            instructorLinks.Where(x => !selectedInstructors.Contains(x.InstructorId)));
        foreach (int instructorId in selectedInstructors.Except(currentInstructors))
            _db.InstructorTrainings.Add(new InstructorTraining
            {
                TrainingId = id, InstructorId = instructorId
            });

        var prerequisites = await _db.TrainingPrerequisites
            .Where(x => x.TrainingId == id).ToListAsync();
        var selectedPrerequisites = model.SelectedPrerequisiteIds.ToHashSet();
        var currentPrerequisites = prerequisites.Select(x => x.PrerequisiteTrainingId).ToHashSet();
        _db.TrainingPrerequisites.RemoveRange(
            prerequisites.Where(x => !selectedPrerequisites.Contains(x.PrerequisiteTrainingId)));
        foreach (int prerequisiteId in selectedPrerequisites.Except(currentPrerequisites))
            _db.TrainingPrerequisites.Add(new TrainingPrerequisite
            {
                TrainingId = id, PrerequisiteTrainingId = prerequisiteId
            });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }
}
