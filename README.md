# TT Planner

## Technical Training Session Optimization

TT Planner is an ASP.NET Core MVC application that identifies technical training needs and assigns participants to predefined training sessions. It combines training history, renewal rules, participant availability, and operational staffing constraints to support planning decisions.

The project was developed as an internship pilot using synthetic data.

## Features

- **Dashboard:** Summarizes overdue and upcoming training needs, critical needs by organizational unit, shift risks, and upcoming sessions.
- **Training needs analysis:** Evaluates completion history and training rules to identify overdue, upcoming, and not-yet-completed needs.
- **Priority scoring:** Calculates a priority score based on the urgency of each participant's training need.
- **Session management and calendar:** Displays predefined sessions and their details.
- **Participant assignment:** Uses Greedy and CP-SAT methods to assign participants to eligible sessions.
- **Planning results and reports:** Displays assignments, unassigned needs and their reasons, and comparisons between planning methods.

## Planning Constraints

Participant assignments consider:

- Session capacity.
- Training prerequisites.
- Participant availability.
- Time conflicts between assigned sessions.
- Minimum staffing requirements for shifts.
- Eligibility rules based on job role and personnel status.

Sessions are defined before optimization. The planning methods select participant-to-session assignments within those sessions.

## Planning Methods

### Greedy

The Greedy method considers training needs in priority order and makes feasible assignments step by step. Earlier decisions affect the options available for later needs.

### CP-SAT

The CP-SAT method uses Google OR-Tools to model participant-to-session assignment decisions and planning constraints together. Solution quality and solver status depend on the scenario and solver settings; a feasible result does not necessarily mean optimality has been proven.

The application supports comparing the two methods on the same planning scenario.

## Technology Stack

| Component | Technology |
| --- | --- |
| Language | C# |
| Framework | ASP.NET Core MVC / .NET 10 |
| Views | Razor, HTML, CSS, JavaScript |
| UI | Bootstrap |
| Database | PostgreSQL |
| Data access | Entity Framework Core with Npgsql |
| Optimization | Google OR-Tools CP-SAT |

## Local Setup

### Prerequisites

- .NET 10 SDK.
- Visual Studio with support for the project's .NET target and the ASP.NET workload.
- PostgreSQL running locally, with a database and a user that can create the application tables.

### 1. Open the project

Clone or download this repository. Open its solution file (`.sln` or `.slnx`) or `TechnicalTrainingPlanner.csproj` in Visual Studio, and restore the NuGet packages.

### 2. Configure the database connection

In Solution Explorer, right-click the **TechnicalTrainingPlanner** project and select **Manage User Secrets**.

Add the following configuration to the opened `secrets.json` file, preserving any existing settings:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=YOUR_DATABASE;Username=YOUR_USERNAME;Password=YOUR_PASSWORD"
  }
}
```

Replace the placeholders with your own local PostgreSQL settings. The connection key must remain `DefaultConnection`, as this is the key read by the application.

User Secrets are stored outside the project directory and are not included in a normal repository commit. Each developer configures their own local connection. Keep real credentials out of tracked configuration files.

### 3. Apply the database migrations

For a fresh local database, open **Tools > NuGet Package Manager > Package Manager Console**. Select the project containing `AppDbContext` and the migrations as the default project, and use **TechnicalTrainingPlanner** as the startup project.

Run:

```powershell
Update-Database -Args '--environment Development'
```

This command requires the Entity Framework Core Package Manager Console tools. If they are missing, install `Microsoft.EntityFrameworkCore.Tools` using a version compatible with the project's EF Core packages.

Migrations create the database schema. Populating the database with pilot records depends on the project's data initialization code; migrations alone do not guarantee that sample records will be present.

### 4. Run the application

Run the project in the **Development** environment using Visual Studio. With the default ASP.NET Core configuration, this environment loads the connection stored in User Secrets.

Open the local URL shown by Visual Studio.

## Typical Workflow

1. Review the training catalog, completion records, and predefined sessions.
2. Select the planning month and organizational units for needs analysis.
3. Review calculated training needs, deadlines, and priority scores.
4. Run a planning scenario using Greedy or CP-SAT.
5. Review assignments and reasons for unassigned needs.
6. Compare planning methods through the reporting screens.

## Pilot Data

The pilot uses fictional personnel and training records. Training rules and operational constraints are modeled for the project scenario. Planning results demonstrate the application's behavior on those inputs.

## Author

Havvanur Akkaya — Mathematical Engineering, Yildiz Technical University.
