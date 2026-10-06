using Microsoft.EntityFrameworkCore;
using TechnicalTrainingPlanner.Domain.Entities;

namespace TechnicalTrainingPlanner.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<JobRole> JobRoles => Set<JobRole>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<PersonnelStatus> PersonnelStatuses => Set<PersonnelStatus>();
    public DbSet<EmployeePersonnelStatus> EmployeePersonnelStatuses => Set<EmployeePersonnelStatus>();
    public DbSet<Training> Trainings => Set<Training>();
    public DbSet<TrainingRequirement> TrainingRequirements => Set<TrainingRequirement>();
    public DbSet<TrainingAudienceStatus> TrainingAudienceStatuses => Set<TrainingAudienceStatus>();
    public DbSet<TrainingPrerequisite> TrainingPrerequisites => Set<TrainingPrerequisite>();
    public DbSet<EmployeeTraining> EmployeeTrainings => Set<EmployeeTraining>();
    public DbSet<EmployeeAvailability> EmployeeAvailabilities => Set<EmployeeAvailability>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<InstructorTraining> InstructorTrainings => Set<InstructorTraining>();
    public DbSet<Classroom> Classrooms => Set<Classroom>();
    public DbSet<TrainingSession> TrainingSessions => Set<TrainingSession>();
    public DbSet<NeedAnalysisRun> NeedAnalysisRuns => Set<NeedAnalysisRun>();
    public DbSet<NeedAnalysisDepartment> NeedAnalysisDepartments => Set<NeedAnalysisDepartment>();
    public DbSet<TrainingNeed> TrainingNeeds => Set<TrainingNeed>();
    public DbSet<OptimizationRun> OptimizationRuns => Set<OptimizationRun>();
    public DbSet<TrainingAssignment> TrainingAssignments => Set<TrainingAssignment>();
    public DbSet<UnassignedReason> UnassignedReasons => Set<UnassignedReason>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Department>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<JobRole>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Employee>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Training>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Instructor>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Classroom>().HasIndex(x => x.Code).IsUnique();

        modelBuilder.Entity<PersonnelStatus>().HasKey(x => x.Code);
        modelBuilder.Entity<EmployeePersonnelStatus>()
            .HasKey(x => new { x.EmployeeId, x.PersonnelStatusCode });
        modelBuilder.Entity<TrainingRequirement>()
            .HasKey(x => new { x.JobRoleId, x.TrainingId });
        modelBuilder.Entity<TrainingAudienceStatus>()
            .HasKey(x => new { x.TrainingId, x.PersonnelStatusCode });
        modelBuilder.Entity<InstructorTraining>()
            .HasKey(x => new { x.InstructorId, x.TrainingId });
        modelBuilder.Entity<NeedAnalysisDepartment>()
            .HasKey(x => new { x.NeedAnalysisRunId, x.DepartmentId });

        modelBuilder.Entity<TrainingPrerequisite>(entity =>
        {
            entity.HasKey(x => new { x.TrainingId, x.PrerequisiteTrainingId });
            entity.HasOne(x => x.Training)
                .WithMany()
                .HasForeignKey(x => x.TrainingId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PrerequisiteTraining)
                .WithMany()
                .HasForeignKey(x => x.PrerequisiteTrainingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TrainingNeed>()
            .HasIndex(x => new { x.NeedAnalysisRunId, x.EmployeeId, x.TrainingId })
            .IsUnique();
        modelBuilder.Entity<TrainingAssignment>()
            .HasIndex(x => new { x.OptimizationRunId, x.TrainingNeedId })
            .IsUnique();
        modelBuilder.Entity<UnassignedReason>()
            .HasIndex(x => new { x.OptimizationRunId, x.TrainingNeedId, x.ReasonCode })
            .IsUnique();
        modelBuilder.Entity<OptimizationRun>()
            .HasIndex(x => x.NeedAnalysisRunId)
            .IsUnique()
            .HasFilter("\"IsSelectedResult\" = true");
    }
}
