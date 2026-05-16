## Implement Cloudsoft Applications and Admin Dashboard

### Goal
Add candidate job applications and an authenticated admin dashboard using the current Cloudsoft-Job architecture.

### Implemented Architecture

- `Cloudsoft.Core.Models.JobApplication` stores candidate application data.
- `IJobApplicationRepository` persists applications through the same repository pattern as job postings.
- `JobApplicationRepository` stores applications in the shared in-memory database.
- `MongoJobApplicationRepository` and `ResilientJobApplicationRepository` mirror the existing MongoDB/fallback pattern.
- `IJobApplicationService` owns business rules for submissions.
- `ICountryLookupService` provides the country dropdown used by the job details page.
- `ICvStorageService` abstracts CV file storage.
- `LocalCvStorageService` writes uploaded CVs under `wwwroot/uploads/cvs`.

### Submit Application Flow

1. A candidate opens `/Job/Details/{id}`.
2. `JobsController.Details` loads the job posting and country list.
3. The Razor view renders the application form with multipart upload support.
4. The form posts to `/Job/Apply`.
5. `JobsController.Apply` validates that a CV was attached and delegates to `IJobApplicationService`.
6. `JobApplicationService.SubmitAsync` validates:
   - The job exists.
   - The job is active.
   - The deadline has not passed.
   - The country code is known.
   - The same email has not already applied for the same job.
7. The CV is saved through `ICvStorageService`.
8. The application is persisted through `IJobApplicationRepository`.
9. The user is redirected back to the job details page with `TempData` feedback.

### Admin Dashboard Flow

1. Authenticated employers can open `/Admin`.
2. `AdminController.Index` loads all job postings and all applications.
3. `Views/Admin/Index.cshtml` shows job status and submitted applications.
4. `AdminController.Create` and `Views/Admin/Create.cshtml` provide an admin job creation path using the existing `JobPosting` model and `IJobPostingService`.

### Files Added

- `src/Cloudsoft.Core/Models/JobApplication.cs`
- `src/Cloudsoft.Core/Models/CountryItem.cs`
- `src/Cloudsoft.Core/Repositories/Interfaces/IJobApplicationRepository.cs`
- `src/Cloudsoft.Core/Repositories/JobApplicationRepository.cs`
- `src/Cloudsoft.Core/Data/MongoDb/MongoJobApplicationRepository.cs`
- `src/Cloudsoft.Core/Repositories/ResilientJobApplicationRepository.cs`
- `src/Cloudsoft.Core/Services/Interfaces/IJobApplicationService.cs`
- `src/Cloudsoft.Core/Services/JobApplicationService.cs`
- `src/Cloudsoft.Core/Services/Interfaces/ICountryLookupService.cs`
- `src/Cloudsoft.Core/Services/CountryLookupService.cs`
- `src/Cloudsoft.Core/Storage/ICvStorageService.cs`
- `src/Cloudsoft.Web/Services/LocalCvStorageService.cs`
- `src/Cloudsoft.Web/Controllers/AdminController.cs`
- `src/Cloudsoft.Web/Views/Admin/Index.cshtml`
- `src/Cloudsoft.Web/Views/Admin/Create.cshtml`

### Files Updated

- `src/Cloudsoft.Web/Controllers/JobsController.cs`
- `src/Cloudsoft.Web/Views/Jobs/Details.cshtml`
- `src/Cloudsoft.Web/Program.cs`
- `src/Cloudsoft.Web/Views/Shared/_Layout.cshtml`
- `src/Cloudsoft.Core/Data/InMemory/IInMemoryDatabase.cs`
- `src/Cloudsoft.Core/Data/InMemory/InMemoryDatabase.cs`
- `src/Cloudsoft.Core/Data/MongoDb/MongoDbMappings.cs`
- `src/Cloudsoft.Core/Options/MongoDbOptions.cs`
- `test/Cloudsoft.Tests/Unit/ControllerTestHelpers.cs`
- `test/Cloudsoft.Tests/Unit/WebJobsControllerTests.cs`

### Notes

The implementation follows the existing Cloudsoft-Job patterns instead of the template namespace names. The template's `IApplicationService`, `IJobService`, and domain entities are mapped to this codebase as `IJobApplicationService`, `IJobPostingService`, and `Cloudsoft.Core.Models`.
