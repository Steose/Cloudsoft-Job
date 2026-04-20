# Project Modification Guide

This document explains how to modify the ASP.NET Core application structure in the `Cloudsoft-job` repository. It includes recommended steps and common commands for updating projects, solutions, and folder structure safely.

## 1. Review the current repository structure

The main solution and projects are:

- `CloudsoftJob.sln`
- `src/CloudsoftJob.Web/CloudsoftJob.Web.csproj`
- `src/CloudsoftJob.Application/CloudsoftJob.Application.csproj`
- `src/CloudsoftJob.Domain/CloudsoftJob.Domain.csproj`
- `src/CloudsoftJob.Infrastructure/CloudsoftJob.Infrastructure.csproj`
- `tests/CloudsoftJob.Test/CloudsoftJob.Test.csproj`
- `tests/CloudsoftJob.IntegrationTests/`

The web app contains MVC controllers, views, models, and static assets.

## 2. Add a new project to the solution

When adding a new project, use the .NET CLI from the repository root.

```bash
cd /Users/stephenucheosedumme/Cloudsoft-job
```

Create a new project, for example a class library:

```bash
dotnet new classlib -o src/CloudsoftJob.NewFeature -n CloudsoftJob.NewFeature
```

Add the new project to the solution:

```bash
dotnet sln CloudsoftJob.sln add src/CloudsoftJob.NewFeature/CloudsoftJob.NewFeature.csproj
```

## 3. Add a project reference

To make the new project available to another project, add a reference:

```bash
dotnet add src/CloudsoftJob.Web/CloudsoftJob.Web.csproj reference src/CloudsoftJob.NewFeature/CloudsoftJob.NewFeature.csproj
```

If the new project is a shared library that should be used by the application layer, add the reference there instead:

```bash
dotnet add src/CloudsoftJob.Application/CloudsoftJob.Application.csproj reference src/CloudsoftJob.NewFeature/CloudsoftJob.NewFeature.csproj
```

## 4. Rename or move files and folders

When renaming or moving files, update namespaces and references.

Example commands to rename a folder and a file:

```bash
cd /Users/stephenucheosedumme/Cloudsoft-job
mv src/CloudsoftJob.Web/Controllers/OldController.cs src/CloudsoftJob.Web/Controllers/NewController.cs
mv src/CloudsoftJob.Web/Views/Old /Users/stephenucheosedumme/Cloudsoft-job/src/CloudsoftJob.Web/Views/New
```

Then update the C# namespace inside the renamed file(s) to match the folder structure.

## 5. Update `Program.cs` or dependency injection registration

The app uses standard ASP.NET Core startup code in `src/CloudsoftJob.Web/Program.cs`.

If you add a new service, register it in DI:

```csharp
builder.Services.AddScoped<IMyService, MyService>();
```

If you add a new repository, register it inside the same project or in the infrastructure project if appropriate:

```csharp
builder.Services.AddScoped<IJobPostingRepository, JobPostingRepository>();
```

## 6. Modify the solution file manually only when needed

If a `dotnet sln` command does not work, you can edit `CloudsoftJob.sln` by hand. Keep project GUIDs and paths consistent.

Use `dotnet sln list` to confirm current solution contents:

```bash
dotnet sln CloudsoftJob.sln list
```

## 7. Add or update tests

Add new unit tests under `tests/CloudsoftJob.Test/` or integration tests under `tests/CloudsoftJob.IntegrationTests/`.

Create a new test file, then run:

```bash
dotnet test CloudsoftJob.sln
```

## 8. Run build and validate changes

After modifying the project structure, rebuild the solution to catch issues:

```bash
dotnet build CloudsoftJob.sln
```

Run tests:

```bash
dotnet test CloudsoftJob.sln
```

## 9. Update documentation and version control

- Add a short summary of the structural change in `README.md` or `docs/user-stories.md` if it affects architecture.
- Commit changes with a descriptive message.

Example Git commands:

```bash
git add .
git commit -m "Modify project structure: add CloudsoftJob.NewFeature and update references"
git push
```

## 10. Common file modifications by area

- `src/CloudsoftJob.Web/Controllers/`: add or update MVC controllers.
- `src/CloudsoftJob.Web/Views/`: add or update Razor views.
- `src/CloudsoftJob.Web/Models/`: add view models or request models.
- `src/CloudsoftJob.Application/Services/`: add application services or business logic.
- `src/CloudsoftJob.Infrastructure/Repositories/`: add or change repository implementations.
- `src/CloudsoftJob.Domain/Entities/`: add domain entities or value objects.

## 11. Notes for this repository

- The web project is the entry point and depends on the application, infrastructure, and domain layers.
- Infrastructure contains data access and configuration options.
- The current project uses both in-memory and MongoDB repository implementations.
- Keep folder paths and namespaces aligned with project conventions.

---

This guide is intended to help you safely modify the app project structure and maintain a working solution after each change.
