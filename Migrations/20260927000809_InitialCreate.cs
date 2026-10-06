using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TechnicalTrainingPlanner.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Classrooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    LocationCode = table.Column<string>(type: "text", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classrooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    LocationCode = table.Column<string>(type: "text", nullable: false),
                    MinOnDuty = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Instructors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    LocationCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instructors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NeedAnalysisRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlanningStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PlanningEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    LookAheadDays = table.Column<int>(type: "integer", nullable: false),
                    ScenarioFingerprint = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NeedAnalysisRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonnelStatuses",
                columns: table => new
                {
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonnelStatuses", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Trainings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    RenewalMonths = table.Column<int>(type: "integer", nullable: true),
                    RuleBasis = table.Column<string>(type: "text", nullable: false),
                    SourceReference = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trainings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DepartmentId = table.Column<int>(type: "integer", nullable: false),
                    JobRoleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Employees_JobRoles_JobRoleId",
                        column: x => x.JobRoleId,
                        principalTable: "JobRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NeedAnalysisDepartments",
                columns: table => new
                {
                    NeedAnalysisRunId = table.Column<int>(type: "integer", nullable: false),
                    DepartmentId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NeedAnalysisDepartments", x => new { x.NeedAnalysisRunId, x.DepartmentId });
                    table.ForeignKey(
                        name: "FK_NeedAnalysisDepartments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NeedAnalysisDepartments_NeedAnalysisRuns_NeedAnalysisRunId",
                        column: x => x.NeedAnalysisRunId,
                        principalTable: "NeedAnalysisRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OptimizationRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NeedAnalysisRunId = table.Column<int>(type: "integer", nullable: false),
                    Method = table.Column<string>(type: "text", nullable: false),
                    SolveStatus = table.Column<string>(type: "text", nullable: false),
                    ScenarioFingerprint = table.Column<string>(type: "text", nullable: false),
                    TotalScore = table.Column<int>(type: "integer", nullable: false),
                    IsSelectedResult = table.Column<bool>(type: "boolean", nullable: false),
                    SettingsJson = table.Column<string>(type: "text", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptimizationRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OptimizationRuns_NeedAnalysisRuns_NeedAnalysisRunId",
                        column: x => x.NeedAnalysisRunId,
                        principalTable: "NeedAnalysisRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InstructorTrainings",
                columns: table => new
                {
                    InstructorId = table.Column<int>(type: "integer", nullable: false),
                    TrainingId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstructorTrainings", x => new { x.InstructorId, x.TrainingId });
                    table.ForeignKey(
                        name: "FK_InstructorTrainings_Instructors_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "Instructors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InstructorTrainings_Trainings_TrainingId",
                        column: x => x.TrainingId,
                        principalTable: "Trainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingAudienceStatuses",
                columns: table => new
                {
                    TrainingId = table.Column<int>(type: "integer", nullable: false),
                    PersonnelStatusCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingAudienceStatuses", x => new { x.TrainingId, x.PersonnelStatusCode });
                    table.ForeignKey(
                        name: "FK_TrainingAudienceStatuses_PersonnelStatuses_PersonnelStatusC~",
                        column: x => x.PersonnelStatusCode,
                        principalTable: "PersonnelStatuses",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingAudienceStatuses_Trainings_TrainingId",
                        column: x => x.TrainingId,
                        principalTable: "Trainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingPrerequisites",
                columns: table => new
                {
                    TrainingId = table.Column<int>(type: "integer", nullable: false),
                    PrerequisiteTrainingId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingPrerequisites", x => new { x.TrainingId, x.PrerequisiteTrainingId });
                    table.ForeignKey(
                        name: "FK_TrainingPrerequisites_Trainings_PrerequisiteTrainingId",
                        column: x => x.PrerequisiteTrainingId,
                        principalTable: "Trainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingPrerequisites_Trainings_TrainingId",
                        column: x => x.TrainingId,
                        principalTable: "Trainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainingRequirements",
                columns: table => new
                {
                    JobRoleId = table.Column<int>(type: "integer", nullable: false),
                    TrainingId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingRequirements", x => new { x.JobRoleId, x.TrainingId });
                    table.ForeignKey(
                        name: "FK_TrainingRequirements_JobRoles_JobRoleId",
                        column: x => x.JobRoleId,
                        principalTable: "JobRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingRequirements_Trainings_TrainingId",
                        column: x => x.TrainingId,
                        principalTable: "Trainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingId = table.Column<int>(type: "integer", nullable: false),
                    InstructorId = table.Column<int>(type: "integer", nullable: false),
                    ClassroomId = table.Column<int>(type: "integer", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingSessions_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingSessions_Instructors_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "Instructors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingSessions_Trainings_TrainingId",
                        column: x => x.TrainingId,
                        principalTable: "Trainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAvailabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    WindowType = table.Column<string>(type: "text", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAvailabilities_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeePersonnelStatuses",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    PersonnelStatusCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeePersonnelStatuses", x => new { x.EmployeeId, x.PersonnelStatusCode });
                    table.ForeignKey(
                        name: "FK_EmployeePersonnelStatuses_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeePersonnelStatuses_PersonnelStatuses_PersonnelStatus~",
                        column: x => x.PersonnelStatusCode,
                        principalTable: "PersonnelStatuses",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeTrainings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    TrainingId = table.Column<int>(type: "integer", nullable: false),
                    CompletedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Passed = table.Column<bool>(type: "boolean", nullable: false),
                    EvidenceReference = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeTrainings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeTrainings_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeTrainings_Trainings_TrainingId",
                        column: x => x.TrainingId,
                        principalTable: "Trainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingNeeds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NeedAnalysisRunId = table.Column<int>(type: "integer", nullable: false),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    TrainingId = table.Column<int>(type: "integer", nullable: false),
                    DueOn = table.Column<DateOnly>(type: "date", nullable: true),
                    NeedType = table.Column<string>(type: "text", nullable: false),
                    PriorityScore = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingNeeds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingNeeds_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingNeeds_NeedAnalysisRuns_NeedAnalysisRunId",
                        column: x => x.NeedAnalysisRunId,
                        principalTable: "NeedAnalysisRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingNeeds_Trainings_TrainingId",
                        column: x => x.TrainingId,
                        principalTable: "Trainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OptimizationRunId = table.Column<int>(type: "integer", nullable: false),
                    TrainingNeedId = table.Column<int>(type: "integer", nullable: false),
                    TrainingSessionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingAssignments_OptimizationRuns_OptimizationRunId",
                        column: x => x.OptimizationRunId,
                        principalTable: "OptimizationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingAssignments_TrainingNeeds_TrainingNeedId",
                        column: x => x.TrainingNeedId,
                        principalTable: "TrainingNeeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingAssignments_TrainingSessions_TrainingSessionId",
                        column: x => x.TrainingSessionId,
                        principalTable: "TrainingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UnassignedReasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OptimizationRunId = table.Column<int>(type: "integer", nullable: false),
                    TrainingNeedId = table.Column<int>(type: "integer", nullable: false),
                    ReasonCode = table.Column<string>(type: "text", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnassignedReasons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnassignedReasons_OptimizationRuns_OptimizationRunId",
                        column: x => x.OptimizationRunId,
                        principalTable: "OptimizationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnassignedReasons_TrainingNeeds_TrainingNeedId",
                        column: x => x.TrainingNeedId,
                        principalTable: "TrainingNeeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_Code",
                table: "Classrooms",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Code",
                table: "Departments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAvailabilities_EmployeeId",
                table: "EmployeeAvailabilities",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePersonnelStatuses_PersonnelStatusCode",
                table: "EmployeePersonnelStatuses",
                column: "PersonnelStatusCode");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Code",
                table: "Employees",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartmentId",
                table: "Employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_JobRoleId",
                table: "Employees",
                column: "JobRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTrainings_EmployeeId",
                table: "EmployeeTrainings",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTrainings_TrainingId",
                table: "EmployeeTrainings",
                column: "TrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_Instructors_Code",
                table: "Instructors",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstructorTrainings_TrainingId",
                table: "InstructorTrainings",
                column: "TrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRoles_Code",
                table: "JobRoles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NeedAnalysisDepartments_DepartmentId",
                table: "NeedAnalysisDepartments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationRuns_NeedAnalysisRunId",
                table: "OptimizationRuns",
                column: "NeedAnalysisRunId",
                unique: true,
                filter: "\"IsSelectedResult\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignments_OptimizationRunId_TrainingNeedId",
                table: "TrainingAssignments",
                columns: new[] { "OptimizationRunId", "TrainingNeedId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignments_TrainingNeedId",
                table: "TrainingAssignments",
                column: "TrainingNeedId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignments_TrainingSessionId",
                table: "TrainingAssignments",
                column: "TrainingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAudienceStatuses_PersonnelStatusCode",
                table: "TrainingAudienceStatuses",
                column: "PersonnelStatusCode");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingNeeds_EmployeeId",
                table: "TrainingNeeds",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingNeeds_NeedAnalysisRunId_EmployeeId_TrainingId",
                table: "TrainingNeeds",
                columns: new[] { "NeedAnalysisRunId", "EmployeeId", "TrainingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingNeeds_TrainingId",
                table: "TrainingNeeds",
                column: "TrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPrerequisites_PrerequisiteTrainingId",
                table: "TrainingPrerequisites",
                column: "PrerequisiteTrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRequirements_TrainingId",
                table: "TrainingRequirements",
                column: "TrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_Trainings_Code",
                table: "Trainings",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_ClassroomId",
                table: "TrainingSessions",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_InstructorId",
                table: "TrainingSessions",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_TrainingId",
                table: "TrainingSessions",
                column: "TrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_UnassignedReasons_OptimizationRunId_TrainingNeedId_ReasonCo~",
                table: "UnassignedReasons",
                columns: new[] { "OptimizationRunId", "TrainingNeedId", "ReasonCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnassignedReasons_TrainingNeedId",
                table: "UnassignedReasons",
                column: "TrainingNeedId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeAvailabilities");

            migrationBuilder.DropTable(
                name: "EmployeePersonnelStatuses");

            migrationBuilder.DropTable(
                name: "EmployeeTrainings");

            migrationBuilder.DropTable(
                name: "InstructorTrainings");

            migrationBuilder.DropTable(
                name: "NeedAnalysisDepartments");

            migrationBuilder.DropTable(
                name: "TrainingAssignments");

            migrationBuilder.DropTable(
                name: "TrainingAudienceStatuses");

            migrationBuilder.DropTable(
                name: "TrainingPrerequisites");

            migrationBuilder.DropTable(
                name: "TrainingRequirements");

            migrationBuilder.DropTable(
                name: "UnassignedReasons");

            migrationBuilder.DropTable(
                name: "TrainingSessions");

            migrationBuilder.DropTable(
                name: "PersonnelStatuses");

            migrationBuilder.DropTable(
                name: "OptimizationRuns");

            migrationBuilder.DropTable(
                name: "TrainingNeeds");

            migrationBuilder.DropTable(
                name: "Classrooms");

            migrationBuilder.DropTable(
                name: "Instructors");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "NeedAnalysisRuns");

            migrationBuilder.DropTable(
                name: "Trainings");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "JobRoles");
        }
    }
}
