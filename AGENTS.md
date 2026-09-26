# Repository Guidelines

## Project Structure

WorkStack is currently a .NET 10 ASP.NET Core MVC starter application with ASP.NET Core Identity. The single web project is `WorkStack.csproj`, in solution `WorkStack.slnx`. `Program.cs` configures services and the request pipeline. MVC code is in `Controllers/`, view models in `Models/`, Razor views in `Views/`, Identity UI pages in `Areas/Identity/Pages/`, and static files in `wwwroot/`. `Data/` contains `ApplicationDbContext` and EF Core migrations. At present, the only controller is `HomeController`, and the only model is `ErrorViewModel`.

## Build and Run

- `dotnet restore WorkStack.slnx` restores NuGet packages.
- `dotnet build WorkStack.slnx` builds the solution.
- `dotnet run --project WorkStack.csproj` runs the web application.
- `dotnet test WorkStack.slnx` runs available tests; no test project is currently present.

The project targets `net10.0`, enables nullable reference types and implicit usings, and references ASP.NET Core and EF Core 10.0.9 packages.

## Current Identity and Database Setup

Identity uses `IdentityUser`, `AddDefaultIdentity`, and EF stores backed by `ApplicationDbContext : IdentityDbContext`. Confirmed accounts are not required by the configured sign-in options. The checked-in `DefaultConnection` uses SQL Server LocalDB. The initial migration creates the Identity schema only. Migrations are not applied automatically at application startup; development exposes the migrations endpoint.

Keep secrets out of committed configuration. Use User Secrets locally or environment configuration for deployment. Review generated migrations before committing them.

## Coding and Change Guidelines

Follow the existing C# style: four spaces, PascalCase for types and public members, and camelCase for locals and parameters. Keep MVC controllers focused on request handling, place presentation in Razor views, and use view models when useful. No formatter, linter, or test framework is currently configured; preserve nearby formatting and build after changes.

## Planned WorkStack Domain

The following describes intended domain design, not implemented code or database schema. Future domain entities are planned as `Workspace`, `WorkspaceMember`, `Board`, `List`, `Task`, `TaskAssignee`, and `Comment`, with `WorkspaceRole` and `TaskPriority` enums. Identity's `AspNetUsers` remains the planned user source; do not add a duplicate user table.

Planned rules: workspaces have owners and members; membership is unique by workspace and user; boards belong to workspaces, lists to boards, tasks to lists, and comments to tasks and users. A task's list represents its status. Tasks may have multiple assignees, who must be workspace members. When implementing this domain, define and review foreign keys, indexes, uniqueness, and delete behavior in EF Core migrations. Do not treat these rules as existing database constraints until implemented.

## Planned Database Constraints

The approved target database design includes the following constraints:

- `WorkspaceMember` must have a unique `(WorkspaceId, UserId)` combination.
- `TaskAssignee` uses `(TaskId, UserId)` as its composite primary key.
- Task status is represented by `Task.ListId`; do not add a separate `Status` column.
- A task assignee must belong to the workspace containing the task.
- Do not create a separate user table; ASP.NET Core Identity `AspNetUsers` is the user source.

### Planned Delete Behavior

- Workspace → Board: Cascade
- Board → List: Cascade
- List → Task: Restrict
- Workspace → WorkspaceMember: Cascade
- Task → TaskAssignee: Cascade
- Task → Comment: Cascade
- User → Workspace: Restrict
- User → Task.Creator: Restrict
- User → Comment: Restrict

These are approved target design decisions and may not yet exist in the current EF Core model.
Do not change them without discussing the impact first.

## Commits and Pull Requests

Use concise imperative commit subjects (for example, `Add workspace model`). Pull requests should describe the change, link related issues, summarize validation, and include screenshots for UI changes. Call out schema or configuration changes.
