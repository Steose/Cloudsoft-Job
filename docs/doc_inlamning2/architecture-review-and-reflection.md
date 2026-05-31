# Architecture Review And Reflection

Layered ASP.NET Core MVC application with Domain, Application, Infrastructure, and Web projects.(Api, Core, Web, Test)

## Cloudsoft-Job System Diagram

![Cloudsoft diagram](../Images/cloudsoft-job-system-diagram.png)

## Architecture Review

Cloudsoft Job is a layered ASP.NET Core application with a web front end, an API surface, a core service layer, repository abstractions, and Azure infrastructure defined in Bicep. The system is split so that HTTP concerns stay in the MVC and API projects, business operations stay in `Cloudsoft.Core`, and deployment concerns stay in `infra/container-apps`.

The MVC layer is the main user-facing path. `JobsController` handles job listing, job details, employer job creation, job activation changes, and applicant CV submission. The API layer exposes job data through `src/Cloudsoft.Api/Controllers/JobsController.cs`, using DTOs from `src/Cloudsoft.Api/Dtos`. Both layers depend on service interfaces such as `IJobPostingService` and `IJobApplicationService` instead of directly talking to persistence. That keeps request handling thin and lets the service layer own business rules.

The core service layer coordinates validation and persistence. `JobApplicationService.SubmitAsync` is the clearest example: it trims and validates applicant input, checks that the job exists and is still accepting applications, prevents duplicate applications, saves the CV through `ICvStorageService`, and then persists the application through `IJobApplicationRepository`. The service does not need to know whether the CV is written to disk locally or to Azure Blob Storage in production. That responsibility is behind the storage abstraction.

Persistence is built around repository interfaces. `IJobPostingRepository`, `IJobApplicationRepository`, and `IEmployerRepository` hide the storage implementation from the service layer. The app can run with in-memory repositories for local development and tests, or MongoDB-backed repositories for Azure. The resilient repository implementations wrap MongoDB repositories and fall back to in-memory storage when MongoDB operations fail, logging the failure with structured `LogError` calls. In Azure, MongoDB is provided by Cosmos DB with the MongoDB API.

The Azure stack is deployed through `infra/container-apps/main.bicep`. The template creates the Container Apps environment, Azure Container Registry, Cosmos DB, Log Analytics, Application Insights, and Blob Storage. The Container App receives configuration through environment variables and secrets. Cosmos DB connection details are passed as a secret, while Blob container URLs are passed as normal configuration because the URL itself is not a credential.

For file upload, the finished design uses the existing job application flow and changes only the storage implementation. Applicants upload CV files through `POST /Job/Apply`. The MVC controller passes the file stream into `JobApplicationService`, which uses `ICvStorageService`. In local development, that storage implementation writes under `wwwroot/uploads/cvs`. In Azure, the production implementation writes the CV to a private `cvs` Blob container. The existing public `images` container remains only for public image assets such as `hero.png`.

Blob Storage access is designed around managed identity rather than connection strings. The Container App has a user-assigned managed identity. The identity is granted `Storage Blob Data Contributor` for the CV container or storage account. The app uses `DefaultAzureCredential`, so Azure SDK calls acquire a token from the Container Apps managed identity at runtime. This means the app does not need a storage account key, shared access signature, or Blob connection string for CV uploads.

The health model separates shallow process health from deep dependency readiness. A deep `/healthz` probe checks whether the app can reach Cosmos DB and Blob Storage. The Cosmos check verifies the configured MongoDB endpoint by running a cheap ping-style operation. The Blob check verifies the configured CV container by reading container properties through the same managed identity path used by uploads. If both checks pass, `/healthz` returns `200 OK`. If either required dependency fails, it returns `503 Service Unavailable`.

Container Apps connects that deep endpoint to the readiness probe. The readiness probe calls `/healthz` on the app container. If Cosmos DB or Blob Storage is unavailable, the probe fails and the replica is marked not ready. Container Apps then removes that replica from traffic rotation. If other replicas are healthy, traffic continues to those replicas. If every replica fails readiness, the revision has no ready backend and users receive service failures until a replica becomes healthy again.

## Reflection

The application now fits together as a conventional layered cloud web application. A request first enters through either the local development server or Azure Container Apps ingress. In Azure, ingress routes the request to a ready Container App replica. ASP.NET Core middleware handles static files, routing, authentication, and authorization before the request reaches MVC or API controllers.

For browser users, MVC controllers are the main entry point. Anonymous users can browse job postings and submit applications. Employer users authenticate with cookie authentication and can create or manage postings.  

![Login flow](../Images/loginFlows.png)

For API consumers, `Cloudsoft.Api.Controllers.JobsController` exposes job data and uses DTOs to avoid returning domain models directly. Both entry points are intentionally thin: they receive HTTP input, call services, and shape HTTP responses.

The service layer is where the application behavior becomes explicit. `JobPostingService` owns job posting operations. `JobApplicationService` owns application submission and CV handling. That keeps validation and workflow decisions out of the controllers. The CV upload path shows the full chain: the browser sends `POST /Job/Apply`, `JobsController.Apply` reads the uploaded `IFormFile`, `JobApplicationService.SubmitAsync` validates the applicant and job state, `ICvStorageService` stores the CV, and `IJobApplicationRepository` saves the application record.

The repository layer is the boundary to data storage. The services depend on repository interfaces, not concrete MongoDB classes. In local development and tests, the app can use in-memory repositories. In Azure, feature flags select MongoDB-backed repositories using Cosmos DB's MongoDB API. The resilient repositories add a pragmatic reliability layer by catching MongoDB failures, logging them, and falling back to in-memory storage. This is useful operationally because it makes dependency problems visible in logs while keeping the application structure simple.

Blob Storage is a separate storage concern from Cosmos DB. Cosmos DB stores structured application data: jobs, employers, and job applications. Blob Storage stores file objects: public images in the `images` container and private CV files in the `cvs` container. This split is appropriate because CVs are binary documents, not database records. The database keeps the metadata and blob name, while Blob Storage owns the file contents.

The deployment architecture follows the same separation. Bicep creates infrastructure, GitHub Actions builds and pushes the container image, and Container Apps runs the app. Configuration flows into the container through environment variables and secrets. `FeatureFlags__UseMongoDb` and `FeatureFlags__UseAzureStorage` decide which infrastructure-backed behavior is active. Application Insights and Log Analytics receive telemetry, while `/healthz` gives the platform a direct readiness signal.

Several design patterns and techniques are visible in the codebase:

- Repository: `IJobPostingRepository`, `IJobApplicationRepository`, and `IEmployerRepository` isolate persistence from business services.
- Dependency Injection: `Program.cs` registers services, repositories, authentication, storage services, options, and telemetry. Controllers and services receive dependencies through constructors.
- DTO: `JobPostingDto`, `CreateJobPostingDto`, and `JobPostingDtoMapping` separate API contracts from domain models.
- Feature Flag: `FeatureFlagsOptions` and `FeatureFlags__UseMongoDb` / `FeatureFlags__UseAzureStorage` switch between local and Azure-backed implementations.
- Managed Identity: the Container App identity pulls from ACR and is the intended identity for Blob Storage access without connection strings.
- Deep Probe: `/healthz` represents dependency-aware readiness by checking Cosmos DB and Blob Storage instead of only checking that the process is alive.
- Middleware: ASP.NET Core middleware handles static files, routing, authentication, authorization, exception handling, HTTPS redirection, and request dispatch before controllers run.

The result is a system where the same app can run locally with simple dependencies and in Azure with managed platform services. The important boundaries are clear: controllers handle HTTP, services handle workflows, repositories handle data access, storage abstractions handle files, and infrastructure code wires the app to Azure services. That makes the application easier to test, deploy, and reason about when a dependency such as Cosmos DB or Blob Storage becomes unavailable.

![Vm deployflow](../Images/vm-deployflow.png)

![ContainerApp deployflow](../Images/container-apps-deployflow.png)

![Test deployflow](../Images/src-test.png)

![Local dev-envflow](../Images/local-development-environment.png)

![Upload featuredataflow](../Images/upload-feature-data-flow.png)
