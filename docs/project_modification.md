# Project Structure Modification Guide

This document explains how to recreate the application structure used in this repository. The goal is to move the existing app into a clean architecture layout while keeping the existing `CloudsoftJob.*` project names.

Target structure:

```text
Cloudsoft-job/
├── src/
│   ├── CloudsoftJob.Web/
│   ├── CloudsoftJob.Application/
│   ├── CloudsoftJob.Domain/
│   └── CloudsoftJob.Infrastructure/
├── tests/
│   ├── CloudsoftJob.Test/
│   └── CloudsoftJob.IntegrationTests/
├── docker/
├── infra/
├── docs/
├── scripts/
└── .github/workflows/
```

## 1. Start From The Repository Root

Run all commands from the repository root:

```bash
cd /Users/stephenucheosedumme/Cloudsoft-job
```

Check the current solution and project files:

```bash
dotnet sln CloudsoftJob.sln list
find src -maxdepth 3 -type f | sort
```

## 2. Create The New Layer Folders

Create the application, domain, infrastructure, test, Docker, infrastructure, documentation, script, and pipeline folders:

```bash
mkdir -p src/CloudsoftJob.Application/Interfaces/Services
mkdir -p src/CloudsoftJob.Application/Interfaces/Repositories
mkdir -p src/CloudsoftJob.Application/Services
mkdir -p src/CloudsoftJob.Application/DTOs

mkdir -p src/CloudsoftJob.Domain/Entities
mkdir -p src/CloudsoftJob.Domain/Enums

mkdir -p src/CloudsoftJob.Infrastructure/Data/InMemory
mkdir -p src/CloudsoftJob.Infrastructure/Data/MongoDb
mkdir -p src/CloudsoftJob.Infrastructure/Data/SQLite
mkdir -p src/CloudsoftJob.Infrastructure/Repositories
mkdir -p src/CloudsoftJob.Infrastructure/Config
mkdir -p src/CloudsoftJob.Infrastructure/Azure/KeyVault
mkdir -p src/CloudsoftJob.Infrastructure/Azure/BlobStorage
mkdir -p src/CloudsoftJob.Infrastructure/Azure/AppInsights
mkdir -p src/CloudsoftJob.Infrastructure/Authentication/GoogleOAuth

mkdir -p src/CloudsoftJob.Web/Services
mkdir -p src/CloudsoftJob.Web/Repositories
mkdir -p src/CloudsoftJob.Web/Middleware
mkdir -p src/CloudsoftJob.Web/Config

mkdir -p tests/CloudsoftJob.IntegrationTests
mkdir -p docker
mkdir -p infra
mkdir -p docs/diagrams
mkdir -p scripts
mkdir -p .github/workflows
```

Add `.gitkeep` files for empty folders that Git should preserve:

```bash
touch src/CloudsoftJob.Application/DTOs/.gitkeep
touch src/CloudsoftJob.Domain/Enums/.gitkeep
touch src/CloudsoftJob.Infrastructure/Data/SQLite/.gitkeep
touch src/CloudsoftJob.Infrastructure/Azure/KeyVault/.gitkeep
touch src/CloudsoftJob.Infrastructure/Azure/BlobStorage/.gitkeep
touch src/CloudsoftJob.Infrastructure/Azure/AppInsights/.gitkeep
touch src/CloudsoftJob.Infrastructure/Authentication/GoogleOAuth/.gitkeep
touch src/CloudsoftJob.Web/Services/.gitkeep
touch src/CloudsoftJob.Web/Repositories/.gitkeep
touch src/CloudsoftJob.Web/Middleware/.gitkeep
touch src/CloudsoftJob.Web/Config/.gitkeep
touch tests/CloudsoftJob.IntegrationTests/.gitkeep
touch docs/diagrams/.gitkeep
```

## 3. Move Domain Entities

Move entity classes from the old core project into the domain project:

```bash
mv src/CloudsoftJob.Core/Models/JobPosting.cs src/CloudsoftJob.Domain/Entities/JobPosting.cs
mv src/CloudsoftJob.Core/Models/EmployerUser.cs src/CloudsoftJob.Domain/Entities/EmployerUser.cs
mv src/CloudsoftJob.Core/Models/EmployerAccount.cs src/CloudsoftJob.Domain/Entities/EmployerAccount.cs
```

The namespaces can stay as they are if the requirement is to change structure only and not names.

## 4. Move Application Interfaces And Services

Move service interfaces to the application layer:

```bash
mv src/CloudsoftJob.Core/Services/Interfaces/IJobPostingService.cs src/CloudsoftJob.Application/Interfaces/Services/IJobPostingService.cs
mv src/CloudsoftJob.Core/Services/Interfaces/IEmployerAuthenticationService.cs src/CloudsoftJob.Application/Interfaces/Services/IEmployerAuthenticationService.cs
```

Move repository interfaces to the application layer:

```bash
mv src/CloudsoftJob.Core/Repositories/Interfaces/IJobPostingRepository.cs src/CloudsoftJob.Application/Interfaces/Repositories/IJobPostingRepository.cs
mv src/CloudsoftJob.Core/Repositories/Interfaces/IEmployerRepository.cs src/CloudsoftJob.Application/Interfaces/Repositories/IEmployerRepository.cs
```

Move business services to the application layer:

```bash
mv src/CloudsoftJob.Core/Services/JobPostingService.cs src/CloudsoftJob.Application/Services/JobPostingService.cs
mv src/CloudsoftJob.Core/Services/EmployerAuthenticationService.cs src/CloudsoftJob.Application/Services/EmployerAuthenticationService.cs
```

## 5. Move Infrastructure Code

Move in-memory database code:

```bash
mv src/CloudsoftJob.Core/Data/IInMemoryDatabase.cs src/CloudsoftJob.Infrastructure/Data/InMemory/IInMemoryDatabase.cs
mv src/CloudsoftJob.Core/Data/InMemoryDatabase.cs src/CloudsoftJob.Infrastructure/Data/InMemory/InMemoryDatabase.cs
```

Move in-memory repositories:

```bash
mv src/CloudsoftJob.Core/Repositories/JobPostingRepository.cs src/CloudsoftJob.Infrastructure/Repositories/JobPostingRepository.cs
mv src/CloudsoftJob.Core/Repositories/EmployerRepository.cs src/CloudsoftJob.Infrastructure/Repositories/EmployerRepository.cs
```

Move MongoDB repositories:

```bash
mv src/CloudsoftJob.Core/Repositories/MongoJobPostingRepository.cs src/CloudsoftJob.Infrastructure/Data/MongoDb/MongoJobPostingRepository.cs
mv src/CloudsoftJob.Core/Repositories/MongoEmployerRepository.cs src/CloudsoftJob.Infrastructure/Data/MongoDb/MongoEmployerRepository.cs
```

Move configuration options:

```bash
mv src/CloudsoftJob.Core/Options/MongoDbOptions.cs src/CloudsoftJob.Infrastructure/Config/MongoDbOptions.cs
mv src/CloudsoftJob.Core/Options/FeatureFlagsOptions.cs src/CloudsoftJob.Infrastructure/Config/FeatureFlagsOptions.cs
```

## 6. Create The New Project Files

Create `src/CloudsoftJob.Domain/CloudsoftJob.Domain.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

Create `src/CloudsoftJob.Application/CloudsoftJob.Application.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\CloudsoftJob.Domain\CloudsoftJob.Domain.csproj" />
  </ItemGroup>
</Project>
```

Create `src/CloudsoftJob.Infrastructure/CloudsoftJob.Infrastructure.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Options" Version="10.0.1" />
    <PackageReference Include="MongoDB.Driver" Version="3.7.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\CloudsoftJob.Application\CloudsoftJob.Application.csproj" />
    <ProjectReference Include="..\CloudsoftJob.Domain\CloudsoftJob.Domain.csproj" />
  </ItemGroup>
</Project>
```

Update `src/CloudsoftJob.Web/CloudsoftJob.Web.csproj` so the web app references the new projects:

```xml
<ItemGroup>
  <ProjectReference Include="..\CloudsoftJob.Application\CloudsoftJob.Application.csproj" />
  <ProjectReference Include="..\CloudsoftJob.Domain\CloudsoftJob.Domain.csproj" />
  <ProjectReference Include="..\CloudsoftJob.Infrastructure\CloudsoftJob.Infrastructure.csproj" />
</ItemGroup>
```

## 7. Move Tests To The Tests Folder

Move the existing test project:

```bash
mv src/CloudsoftJob.Test tests/CloudsoftJob.Test
```

Update `tests/CloudsoftJob.Test/CloudsoftJob.Test.csproj` project references to use the new relative paths:

```xml
<ProjectReference Include="..\..\src\CloudsoftJob.Application\CloudsoftJob.Application.csproj" />
<ProjectReference Include="..\..\src\CloudsoftJob.Domain\CloudsoftJob.Domain.csproj" />
```

## 8. Move Docker Files

Move Docker files into the `docker/` folder:

```bash
mv Dockerfile docker/Dockerfile
mv docker-compose.yml docker/docker-compose.yml
mv .dockerignore docker/.dockerignore
```

Because Docker Compose builds from the repository root, add a Dockerfile-specific ignore file beside the Dockerfile:

```bash
cp docker/.dockerignore docker/Dockerfile.dockerignore
```

Update `docker/docker-compose.yml` so the build context points back to the repository root:

```yaml
services:
  web:
    build:
      context: ..
      dockerfile: docker/Dockerfile
```

Update `docker/Dockerfile` so restore and publish use the new paths:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CloudsoftJob.sln .
COPY src/CloudsoftJob.Domain/CloudsoftJob.Domain.csproj src/CloudsoftJob.Domain/
COPY src/CloudsoftJob.Application/CloudsoftJob.Application.csproj src/CloudsoftJob.Application/
COPY src/CloudsoftJob.Infrastructure/CloudsoftJob.Infrastructure.csproj src/CloudsoftJob.Infrastructure/
COPY src/CloudsoftJob.Web/CloudsoftJob.Web.csproj src/CloudsoftJob.Web/
COPY tests/CloudsoftJob.Test/CloudsoftJob.Test.csproj tests/CloudsoftJob.Test/
RUN dotnet restore src/CloudsoftJob.Web/CloudsoftJob.Web.csproj

COPY . .
RUN dotnet restore src/CloudsoftJob.Web/CloudsoftJob.Web.csproj
RUN dotnet publish src/CloudsoftJob.Web/CloudsoftJob.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "CloudsoftJob.Web.dll"]
```

The second restore after `COPY . .` prevents publish errors caused by stale or partial package assets in Docker cache.

## 9. Add Documentation, Scripts, Infra, And Pipeline Files

Create basic project documentation files:

```bash
touch docs/architecture.md
touch docs/user-stories.md
touch docs/api-spec.md
```

Create helper scripts:

```bash
touch scripts/setup.sh
touch scripts/seed-data.sh
chmod +x scripts/setup.sh
chmod +x scripts/seed-data.sh
```

Create infrastructure placeholders:

```bash
touch infra/main.bicep
touch infra/main.bicepparam
touch infra/cloud-init-nginx.yaml
touch infra/provision.sh
chmod +x infra/provision.sh
```

Create the GitHub Actions workflow:

```bash
touch .github/workflows/ci-cd.yml
```

## 10. Update The Solution File

Remove old project paths:

```bash
dotnet sln CloudsoftJob.sln remove src/CloudsoftJob.Core/CloudsoftJob.Core.csproj
dotnet sln CloudsoftJob.sln remove src/CloudsoftJob.Test/CloudsoftJob.Test.csproj
```

Add the new project paths:

```bash
dotnet sln CloudsoftJob.sln add src/CloudsoftJob.Domain/CloudsoftJob.Domain.csproj
dotnet sln CloudsoftJob.sln add src/CloudsoftJob.Application/CloudsoftJob.Application.csproj
dotnet sln CloudsoftJob.sln add src/CloudsoftJob.Infrastructure/CloudsoftJob.Infrastructure.csproj
dotnet sln CloudsoftJob.sln add src/CloudsoftJob.Web/CloudsoftJob.Web.csproj
dotnet sln CloudsoftJob.sln add tests/CloudsoftJob.Test/CloudsoftJob.Test.csproj
```

Confirm the solution contents:

```bash
dotnet sln CloudsoftJob.sln list
```

## 11. Remove The Old Core Folder

After all source files are moved and the build works, remove the old core folder:

```bash
rm -rf src/CloudsoftJob.Core
```

## 12. Build And Test

Restore packages:

```bash
dotnet restore CloudsoftJob.sln
```

Build the full solution:

```bash
dotnet build CloudsoftJob.sln --disable-build-servers -m:1
```

Run tests:

```bash
dotnet test CloudsoftJob.sln --no-build --no-restore --disable-build-servers
```

Validate Docker Compose:

```bash
docker compose -f docker/docker-compose.yml config
```

Build and run the app with MongoDB:

```bash
docker compose -f docker/docker-compose.yml up --build
```

## 13. Clean Generated Build Output

After verification, remove generated `bin` and `obj` folders if you want the tree to show only source files:

```bash
rm -rf src/CloudsoftJob.Application/bin src/CloudsoftJob.Application/obj
rm -rf src/CloudsoftJob.Domain/bin src/CloudsoftJob.Domain/obj
rm -rf src/CloudsoftJob.Infrastructure/bin src/CloudsoftJob.Infrastructure/obj
rm -rf src/CloudsoftJob.Web/bin src/CloudsoftJob.Web/obj
rm -rf tests/CloudsoftJob.Test/bin tests/CloudsoftJob.Test/obj
```

## 14. Check The Final Structure

Use these commands to confirm the new structure:

```bash
find src tests docker infra docs scripts .github -maxdepth 4 -type d | sort
git status --short
```

Expected top-level folders:

```text
.github/
docker/
docs/
infra/
scripts/
src/
tests/
```

## 15. Notes

- This structure changes physical file locations, not the app's public behavior.
- Existing `CloudsoftJob.*` names are preserved.
- The web project remains the startup project.
- The application layer owns service interfaces and business services.
- The domain layer owns entities.
- The infrastructure layer owns repository implementations, MongoDB code, in-memory data access, and configuration options.
