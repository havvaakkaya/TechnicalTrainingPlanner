using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TechnicalTrainingPlanner.Domain.Entities;
using TechnicalTrainingPlanner.Infrastructure.Persistence;
using TechnicalTrainingPlanner.ViewModels.Sessions;

namespace TechnicalTrainingPlanner.Application.Sessions;

public class SessionCommandService
{
    private readonly AppDbContext _db;

    public SessionCommandService(AppDbContext db) => _db = db;

    public async Task PopulateChoicesAsync(SessionFormViewModel model)
    {
        var trainings = await _db.Trainings.AsNoTracking().OrderBy(x => x.Code).ToListAsync();
        var instructors = await _db.Instructors.AsNoTracking().OrderBy(x => x.Code).ToListAsync();
        var classrooms = await _db.Classrooms.AsNoTracking().OrderBy(x => x.Code).ToListAsync();
        model.Trainings = trainings
            .Select(x => new SelectListItem($"{x.Code} · {x.Name} ({x.DurationMinutes} dk)", x.Id.ToString()))
            .ToList();
        model.Instructors = instructors
            .Select(x => new SelectListItem($"{x.Code} · {x.LocationCode}", x.Id.ToString()))
            .ToList();
        model.Classrooms = classrooms
            .Select(x => new SelectListItem($"{x.Code} · {x.LocationCode} · {x.Capacity} kişi", x.Id.ToString()))
            .ToList();
    }

    public async Task<SessionFormViewModel?> GetEditFormAsync(int id)
    {
        var record = await _db.TrainingSessions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
        if (record is null) return null;
        var model = new SessionFormViewModel
        {
            Id = record.Id, TrainingId = record.TrainingId,
            InstructorId = record.InstructorId, ClassroomId = record.ClassroomId,
            Capacity = record.Capacity, Status = record.Status,
            StartsAtLocal = PilotTimeZone.ToLocal(record.StartsAtUtc),
            EndsAtLocal = PilotTimeZone.ToLocal(record.EndsAtUtc)
        };
        await PopulateChoicesAsync(model);
        return model;
    }

    public async Task<List<(string Field, string Message)>> ValidateAsync(
        SessionFormViewModel model, int? editingId = null)
    {
        var errors = new List<(string Field, string Message)>();
        if (model.Status is not ("PLANNED" or "CANCELLED"))
            errors.Add((nameof(model.Status), "Geçerli bir oturum durumu seçiniz."));

        var training = await _db.Trainings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == model.TrainingId);
        var instructor = await _db.Instructors.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == model.InstructorId);
        var classroom = await _db.Classrooms.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == model.ClassroomId);
        if (training is null) errors.Add((nameof(model.TrainingId), "Geçerli eğitim seçiniz."));
        if (instructor is null) errors.Add((nameof(model.InstructorId), "Geçerli eğitmen seçiniz."));
        if (classroom is null) errors.Add((nameof(model.ClassroomId), "Geçerli sınıf seçiniz."));
        if (classroom is not null && (model.Capacity < 1 || model.Capacity > classroom.Capacity))
            errors.Add((nameof(model.Capacity), $"Kontenjan 1 ile {classroom.Capacity} arasında olmalıdır."));
        if (instructor is not null && classroom is not null &&
            instructor.LocationCode != classroom.LocationCode)
            errors.Add((nameof(model.ClassroomId), "Eğitmen ve sınıfın konumu aynı olmalıdır."));
        if (training is not null && instructor is not null &&
            !await _db.InstructorTrainings.AsNoTracking().AnyAsync(x =>
                x.TrainingId == training.Id && x.InstructorId == instructor.Id))
            errors.Add((nameof(model.InstructorId), "Bu eğitmen seçilen eğitimi verme yetkisine sahip değil."));

        if (model.StartsAtLocal == default || model.EndsAtLocal == default)
        {
            errors.Add((nameof(model.StartsAtLocal), "Başlangıç ve bitiş saati giriniz."));
            return errors;
        }
        if (model.StartsAtLocal >= model.EndsAtLocal)
            errors.Add((nameof(model.EndsAtLocal), "Bitiş başlangıçtan sonra olmalıdır."));
        if (PilotTimeZone.IsInvalid(model.StartsAtLocal) || PilotTimeZone.IsInvalid(model.EndsAtLocal) ||
            PilotTimeZone.IsAmbiguous(model.StartsAtLocal) || PilotTimeZone.IsAmbiguous(model.EndsAtLocal))
            errors.Add((nameof(model.StartsAtLocal), "Bu yerel saat açık biçimde UTC'ye çevrilemiyor."));
        if (errors.Any(x => x.Field == nameof(model.StartsAtLocal) || x.Field == nameof(model.EndsAtLocal)))
            return errors;

        DateTime startsUtc = PilotTimeZone.ToUtc(model.StartsAtLocal);
        DateTime endsUtc = PilotTimeZone.ToUtc(model.EndsAtLocal);
        if (training is not null && (endsUtc - startsUtc).TotalMinutes != training.DurationMinutes)
            errors.Add((nameof(model.EndsAtLocal),
                $"Oturum süresi {training.DurationMinutes} dakika olmalıdır."));

        if (model.Status == "PLANNED" && instructor is not null && classroom is not null)
        {
            var conflicts = await _db.TrainingSessions.AsNoTracking()
                .Where(x => x.Id != (editingId ?? 0) && x.Status == "PLANNED" &&
                    x.StartsAtUtc < endsUtc && startsUtc < x.EndsAtUtc &&
                    (x.InstructorId == instructor.Id || x.ClassroomId == classroom.Id))
                .Select(x => new { x.InstructorId, x.ClassroomId }).ToListAsync();
            if (conflicts.Any(x => x.InstructorId == instructor.Id))
                errors.Add((nameof(model.InstructorId), "Eğitmen bu saatte başka oturumda görevli."));
            if (conflicts.Any(x => x.ClassroomId == classroom.Id))
                errors.Add((nameof(model.ClassroomId), "Sınıf bu saatte başka oturum için ayrılmış."));
        }

        if (editingId.HasValue && await _db.TrainingAssignments.AsNoTracking()
            .AnyAsync(x => x.TrainingSessionId == editingId.Value))
            errors.Add((string.Empty, "Plan sonucu bulunan oturum düzenlenemez; yeni analiz ve plan gerekir."));
        return errors;
    }

    public async Task<int> CreateAsync(SessionFormViewModel model)
    {
        var session = new TrainingSession
        {
            TrainingId = model.TrainingId, InstructorId = model.InstructorId,
            ClassroomId = model.ClassroomId, Capacity = model.Capacity,
            StartsAtUtc = PilotTimeZone.ToUtc(model.StartsAtLocal),
            EndsAtUtc = PilotTimeZone.ToUtc(model.EndsAtLocal), Status = model.Status
        };
        _db.TrainingSessions.Add(session);
        await _db.SaveChangesAsync();
        return session.Id;
    }

    public async Task<bool> UpdateAsync(int id, SessionFormViewModel model)
    {
        var session = await _db.TrainingSessions.FirstOrDefaultAsync(x => x.Id == id);
        if (session is null) return false;
        session.TrainingId = model.TrainingId;
        session.InstructorId = model.InstructorId;
        session.ClassroomId = model.ClassroomId;
        session.Capacity = model.Capacity;
        session.StartsAtUtc = PilotTimeZone.ToUtc(model.StartsAtLocal);
        session.EndsAtUtc = PilotTimeZone.ToUtc(model.EndsAtLocal);
        session.Status = model.Status;
        await _db.SaveChangesAsync();
        return true;
    }
}
