using Microsoft.EntityFrameworkCore;
using TechnicalTrainingPlanner.Domain.Entities;
using TechnicalTrainingPlanner.Infrastructure.Persistence;

namespace TechnicalTrainingPlanner.Infrastructure.Seed;

public static class PilotDataSeeder
{
    // Sabit pilot ay: aynı veri ve planlama tarihiyle sonuçlar tekrar üretilebilir.
    public static readonly DateOnly PlanningStart = new(2026, 10, 1);
    public static readonly DateOnly PlanningEnd = new(2026, 10, 31);

    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Departments.AnyAsync())
            return; // Daha önce eklenen pilot kayıtları çoğaltma.

        if (await db.Employees.AnyAsync() || await db.Trainings.AnyAsync())
            throw new InvalidOperationException("Veritabanında kısmi veri var; pilot seed uygulanmadı.");

        await using var transaction = await db.Database.BeginTransactionAsync();

        var departments = new[]
        {
            new Department { Code = "D01", Name = "Hat bakım", LocationCode = "L1", MinOnDuty = 5 },
            new Department { Code = "D02", Name = "Üs bakım", LocationCode = "L2", MinOnDuty = 4 },
            new Department { Code = "D03", Name = "Aviyonik", LocationCode = "L1", MinOnDuty = 5 },
            new Department { Code = "D04", Name = "Bileşen", LocationCode = "L2", MinOnDuty = 3 },
            new Department { Code = "D05", Name = "Planlama ve lojistik", LocationCode = "L1", MinOnDuty = 2 }
        };

        var roleNames = new[]
        {
            "Hat bakım teknisyeni", "Üs bakım teknisyeni", "Aviyonik teknisyeni",
            "Bileşen teknisyeni", "Kalite kontrol uzmanı", "Bakım planlama uzmanı",
            "Teknik lojistik personeli", "Atölye takım lideri"
        };
        var roles = roleNames.Select((name, index) => new JobRole
        {
            Code = $"R{index + 1:00}", Name = name
        }).ToArray();

        var trainingNames = new[]
        {
            "SHT-145 tekrarlı eğitim programı", "Emniyet farkındalığı",
            "Dijital bakım kayıtları", "Teknik doküman okuma", "Hat bakım iş akışı",
            "Mekanik sistem kontrolleri", "Aviyonik arıza takibi", "Bileşen muayenesi",
            "Kalite kontrol uygulamaları", "Malzeme izlenebilirliği",
            "Atölye iş güvenliği", "Takım liderliği ve devir teslim",
            "Bakım planlama yazılımı", "Tork uygulaması ve doğrulama",
            "Temel yangın güvenliği"
        };
        int[] recurring24 = { 1, 2, 3, 9, 10, 14, 15 };
        var trainings = trainingNames.Select((name, index) =>
        {
            int number = index + 1;
            return new Training
            {
                Code = $"E{number:00}", Name = name,
                DurationMinutes = number == 1 ? 240 : 120,
                RenewalMonths = number == 11 ? 12 : recurring24.Contains(number) ? 24 : null,
                RuleBasis = number == 1 ? "SHT-145" : "PILOT",
                SourceReference = number == 1 ? "SHT-145 IR 145.A.35(d) ve (e)" : null
            };
        }).ToArray();

        var statuses = new[]
        {
            new PersonnelStatus { Code = "CERTIFYING", Name = "Onaylayıcı personel" },
            new PersonnelStatus { Code = "SUPPORT", Name = "Destek personeli" }
        };
        var instructors = Enumerable.Range(1, 8).Select(i => new Instructor
        {
            Code = $"I{i:00}", LocationCode = i <= 4 ? "L1" : "L2"
        }).ToArray();
        var classrooms = Enumerable.Range(1, 6).Select(i => new Classroom
        {
            Code = $"C{i:00}", LocationCode = i <= 3 ? "L1" : "L2",
            Capacity = new[] { 12, 16, 20 }[(i - 1) % 3]
        }).ToArray();

        db.Departments.AddRange(departments);
        db.JobRoles.AddRange(roles);
        db.Trainings.AddRange(trainings);
        db.PersonnelStatuses.AddRange(statuses);
        db.Instructors.AddRange(instructors);
        db.Classrooms.AddRange(classrooms);
        await db.SaveChangesAsync();

        // Kodlar ve eşleşmeler onaylanan pilot katalogdan alınmıştır.
        var roleTrainingCodes = new[]
        {
            "01,02,03,04,05,14,15", "01,02,03,04,06,14,15",
            "01,02,03,04,07,15", "01,02,03,04,08,11,14",
            "02,04,09", "03,04,13", "03,10,11", "01,02,04,08,11,12,14"
        };
        for (int r = 0; r < roles.Length; r++)
        {
            foreach (string code in roleTrainingCodes[r].Split(','))
                db.TrainingRequirements.Add(new TrainingRequirement
                {
                    JobRoleId = roles[r].Id,
                    TrainingId = trainings[int.Parse(code) - 1].Id
                });
        }

        foreach (var status in statuses)
            db.TrainingAudienceStatuses.Add(new TrainingAudienceStatus
            {
                TrainingId = trainings[0].Id, PersonnelStatusCode = status.Code
            });

        foreach (int n in new[] { 5, 6, 7, 8, 9, 14 })
            AddPrerequisite(n, 4);
        AddPrerequisite(13, 3);

        void AddPrerequisite(int trainingNumber, int prerequisiteNumber)
        {
            db.TrainingPrerequisites.Add(new TrainingPrerequisite
            {
                TrainingId = trainings[trainingNumber - 1].Id,
                PrerequisiteTrainingId = trainings[prerequisiteNumber - 1].Id
            });
        }

        var employees = Enumerable.Range(1, 120).Select(i => new Employee
        {
            Code = $"P{i:000}", DisplayName = $"Pilot Çalışan {i:000}",
            DepartmentId = departments[(i - 1) % departments.Length].Id,
            JobRoleId = roles[((i - 1) / departments.Length) % roles.Length].Id,
            IsActive = true
        }).ToArray();
        db.Employees.AddRange(employees);
        await db.SaveChangesAsync();

        foreach (int i in Enumerable.Range(1, employees.Length))
        {
            var employee = employees[i - 1];
            int roleNumber = ((i - 1) / departments.Length) % roles.Length + 1;
            string? statusCode = roleNumber is 1 or 3 or 4 ? "CERTIFYING"
                : roleNumber is 2 or 8 ? "SUPPORT" : null;
            if (statusCode is not null)
                db.EmployeePersonnelStatuses.Add(new EmployeePersonnelStatus
                {
                    EmployeeId = employee.Id, PersonnelStatusCode = statusCode
                });

            foreach (string code in roleTrainingCodes[roleNumber - 1].Split(','))
            {
                int trainingNumber = int.Parse(code);
                var training = trainings[trainingNumber - 1];
                int bucket = (i * 17 + trainingNumber * 31) % 20;
                if (bucket < 2) continue; // Yaklaşık %10 eksik tamamlama.

                DateOnly completedOn;
                if (training.RenewalMonths is int months)
                {
                    // Yaklaşık %10 gecikmiş, %10 ay içinde gelecek, kalanlar güncel.
                    DateOnly dueOn = bucket < 4 ? PlanningStart.AddDays(-10)
                        : bucket < 6 ? PlanningStart.AddDays(14)
                        : PlanningStart.AddMonths(3);
                    completedOn = dueOn.AddMonths(-months);
                }
                else
                {
                    completedOn = new DateOnly(2025, 5, 10);
                }

                db.EmployeeTrainings.Add(new EmployeeTraining
                {
                    EmployeeId = employee.Id, TrainingId = training.Id,
                    CompletedOn = completedOn, Passed = true,
                    EvidenceReference = $"PILOT-{i:000}-{trainingNumber:00}"
                });
            }
        }

        var workingDays = Enumerable.Range(1, PlanningEnd.Day)
            .Select(day => new DateOnly(PlanningStart.Year, PlanningStart.Month, day))
            .Where(day => day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            .ToArray();

        foreach (int i in Enumerable.Range(1, employees.Length))
        {
            foreach (DateOnly day in workingDays)
            {
                // UTC 06:00–15:00; Ekim 2026 İstanbul gösteriminde 09:00–18:00.
                db.EmployeeAvailabilities.Add(new EmployeeAvailability
                {
                    EmployeeId = employees[i - 1].Id, WindowType = "WORKING_SHIFT",
                    StartsAtUtc = Utc(day, 6), EndsAtUtc = Utc(day, 15)
                });
                if ((i + day.Day) % 29 == 0)
                    db.EmployeeAvailabilities.Add(new EmployeeAvailability
                    {
                        EmployeeId = employees[i - 1].Id,
                        WindowType = "UNAVAILABLE", Reason = "Pilot uygun olmama kaydı",
                        StartsAtUtc = Utc(day, 10), EndsAtUtc = Utc(day, 12)
                    });
            }
        }

        // Her eğitimin her pilot lokasyonda yetkili bir eğitmeni vardır.
        for (int trainingNumber = 1; trainingNumber <= trainings.Length; trainingNumber++)
        {
            for (int location = 0; location < 2; location++)
            {
                var instructor = instructors[location * 4 + (trainingNumber - 1) % 4];
                db.InstructorTrainings.Add(new InstructorTraining
                {
                    InstructorId = instructor.Id,
                    TrainingId = trainings[trainingNumber - 1].Id
                });
            }
        }

        for (int index = 0; index < 25; index++)
        {
            int trainingNumber = index % trainings.Length + 1;
            int location = index % 2;
            var classroom = classrooms[location * 3 + index % 3];
            var instructor = instructors[location * 4 + (trainingNumber - 1) % 4];
            var day = workingDays[index % workingDays.Length];
            int startHourUtc = index < workingDays.Length ? 6 : 10;
            int durationHours = trainingNumber == 1 ? 4 : 2;
            db.TrainingSessions.Add(new TrainingSession
            {
                TrainingId = trainings[trainingNumber - 1].Id,
                InstructorId = instructor.Id,
                ClassroomId = classroom.Id,
                StartsAtUtc = Utc(day, startHourUtc),
                EndsAtUtc = Utc(day, startHourUtc + durationHours),
                Capacity = Math.Min(classroom.Capacity, 8 + index % 3 * 3),
                Status = "PLANNED"
            });
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static DateTime Utc(DateOnly date, int hour) =>
        new(date.Year, date.Month, date.Day, hour, 0, 0, DateTimeKind.Utc);
}
