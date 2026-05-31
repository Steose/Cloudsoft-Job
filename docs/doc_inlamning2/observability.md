# Make the Application Observable

## Goal

Move Cloudsoft Job from "it runs" to "you can see what it is doing."

Observability for this application has three parts:

1. The application writes useful structured logs with `ILogger<T>`.
2. Azure Container Apps forwards container console output to Log Analytics.
3. Application Insights can be added for request traces, dependencies, failures, and richer incident queries.

## Current Implementation

The application now logs the minimum business flows needed for operational visibility:

- Employer login flow in `AccountController`.
- Job create flow in `JobsController`.
- Job status update flow in `JobsController`.

The logs deliberately avoid personal data and secrets.

## Step 1: Use `ILogger<T>` In Controllers

Inject `ILogger<T>` into the controller where the flow happens.

Example code:

```csharp
private readonly ILogger<JobsController> _logger;

public JobsController(
    IJobPostingService jobPostingService,
    IJobApplicationService jobApplicationService,
    ICountryLookupService countryLookupService,
    ILogger<JobsController> logger)
{
    _jobPostingService = jobPostingService;
    _jobApplicationService = jobApplicationService;
    _countryLookupService = countryLookupService;
    _logger = logger;
}
```

## Step 2: Log The Login Flow

The login flow logs:

| Event | Level | Why |
| --- | --- | --- |
| Invalid login model | `Warning` | The request was rejected before authentication. |
| Invalid credentials | `Warning` | A login failed and may indicate user error or probing. |
| Successful login | `Information` | Useful for auditing sign-in activity by employer ID. |
| Logout | `Information` | Useful for understanding session lifecycle. |

Example:

```csharp
_logger.LogWarning(
    "Employer login rejected because the submitted model is invalid. ValidationErrorCount: {ValidationErrorCount}",
    ModelState.ErrorCount);
```

```csharp
_logger.LogWarning("Employer login failed. Reason: {Reason}", "InvalidCredentials");
```

```csharp
_logger.LogInformation("Employer login succeeded. EmployerId: {EmployerId}", employer.Id);
```

## Step 3: Log Create And Update Flows

The job create flow logs:

| Event | Level | Why |
| --- | --- | --- |
| Invalid create model | `Warning` | The request was rejected before persistence. |
| Job created | `Information` | Confirms a business object was created. |

Example:

```csharp
_logger.LogInformation(
    "Job created. JobPostingId: {JobPostingId}, EmployerId: {EmployerId}, DeadlineDate: {DeadlineDate}, IsActive: {IsActive}",
    createdJobPosting.Id,
    createdJobPosting.EmployerId,
    createdJobPosting.Deadline.Date,
    createdJobPosting.IsActive);
```

The job status update flow logs:

| Event | Level | Why |
| --- | --- | --- |
| Job not found during update | `Warning` | The requested update could not be completed. |
| Persistence update failed | `Warning` | The service could not update the job state. |
| Job status updated | `Information` | Confirms the update happened. |

Example:

```csharp
_logger.LogInformation(
    "Job status updated. JobPostingId: {JobPostingId}, EmployerId: {EmployerId}, IsActive: {IsActive}",
    jobPosting.Id,
    jobPosting.EmployerId,
    willBeActive);
```

## Step 4: Used Log Levels

Used levels in this application:

| Level | Use For | Example |
| --- | --- | --- |
| `Trace` | Very detailed local debugging only. | Avoid in normal production settings. |
| `Debug` | Developer diagnostics. | Temporary troubleshooting while developing. |
| `Information` | Successful business events. | Login succeeded, job created, job status updated. |
| `Warning` | Expected but important problems. | Invalid login, invalid form, missing job. |
| `Error` | Operation failed unexpectedly. | Repository fallback after MongoDB failure. |
| `Critical` | App cannot continue safely. | Startup failure, missing required production config. |

Current appsettings keep application logs at `Information` and framework logs at `Warning`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

Because the web app uses `WebApplication.CreateEmptyBuilder`, the logging configuration must be connected explicitly in `Program.cs`. Add the configuration provider before the console provider:

```csharp
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
```

Without `AddConfiguration`, the `Logging` section in `appsettings.json` is loaded into application configuration but is not applied to the logging filters. That can make framework `Information` logs appear even when `Microsoft.AspNetCore` is set to `Warning`.

## Step 5: Do Not Log Sensitive Data

Did not log:

- Passwords.
- API keys.
- MongoDB connection strings.
- Key Vault URIs that include secrets.
- Authentication cookies.
- CV file contents.
- Full names.
- Email addresses.
- Raw request bodies.
- Login form values.

Prefer safe identifiers:

- `EmployerId`
- `JobPostingId`
- `IsActive`
- `DeadlineDate`
- `ValidationErrorCount`
- Reason codes such as `InvalidCredentials`

This gives enough diagnostic value without exposing personal data or secrets.

## Step 6: How Logs Reach Log Analytics

### Current Path: Container Apps Console Logging

The current infrastructure uses Azure Container Apps with Log Analytics.

The Bicep template creates a Log Analytics workspace:

```bicep
resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}
```

The Container Apps environment sends app logs to that workspace:

```bicep
resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: containerAppsEnvironmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
  }
}
```

Because the web app uses configured console logging:

```csharp
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
```

logs written by `ILogger<T>` go to stdout/stderr. Azure Container Apps collects that output and sends it to the Log Analytics table:

```text
ContainerAppConsoleLogs_CL
```

System events such as restarts, revision activation, and scaling go to:

```text
ContainerAppSystemLogs_CL
```

### Optional Path: Application Insights SDK

Application Insights is useful when you need request duration, exceptions, dependency calls, distributed tracing, and application maps.

For ASP.NET Core, install:

```bash
dotnet add src/Cloudsoft.Web package Azure.Monitor.OpenTelemetry.AspNetCore
```

Then update `src/Cloudsoft.Web/Program.cs`.

Add this `using` at the top of the file with the other `using` statements:

```csharp
using Azure.Monitor.OpenTelemetry.AspNetCore;
```

Add the OpenTelemetry registration after `builder.Services.AddControllersWithViews();` and before the application services are registered. Register Azure Monitor only when the connection string is configured, so local Development runs can start without Application Insights:

```csharp
var applicationInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
if (!string.IsNullOrWhiteSpace(applicationInsightsConnectionString))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor();
}
else if (!builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException("APPLICATIONINSIGHTS_CONNECTION_STRING must be configured outside Development.");
}
```

Example placement:

```csharp
// Add services to the container.
builder.Services.AddControllersWithViews();
var applicationInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
if (!string.IsNullOrWhiteSpace(applicationInsightsConnectionString))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor();
}
else if (!builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException("APPLICATIONINSIGHTS_CONNECTION_STRING must be configured outside Development.");
}

builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(MongoDbOptions.SectionName));
```

Set the Application Insights connection string as an environment variable on the running app:

```text
APPLICATIONINSIGHTS_CONNECTION_STRING=<application-insights-connection-string>
```

For this repository, the Azure Container Apps Bicep template configures that value automatically.

`infra/container-apps/main.bicep` creates a workspace-based Application Insights resource:

```bicep
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logs.id
  }
}
```

The same template stores the connection string as a Container Apps secret:

```bicep
{
  name: 'appinsights-connection-string'
  value: appInsights.properties.ConnectionString
}
```

Then it exposes the secret to the container as the environment variable expected by the Azure Monitor OpenTelemetry SDK:

```bicep
{
  name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
  secretRef: 'appinsights-connection-string'
}
```

Deploy the template to create the Application Insights resource and update the Container App configuration:

```bash
az deployment group create \
  --resource-group cloudsoft-job-aca-rg \
  --template-file infra/container-apps/main.bicep \
  --parameters infra/container-apps/main.bicepparam
```

For a local development run with Application Insights enabled, get the connection string from the deployed Application Insights resource and export it in the same shell before starting the web app.

If the Application Insights resource has not been deployed yet, deploy the Bicep template first:

```bash
az deployment group create \
  --resource-group cloudsoft-job-aca-rg \
  --template-file infra/container-apps/main.bicep \
  --parameters infra/container-apps/main.bicepparam
```

Get the connection string:

```bash
az monitor app-insights component show \
  --app cloudsoftjob-appi \
  --resource-group cloudsoft-job-aca-rg \
  --query connectionString \
  -o tsv
```

Export it:

```bash
export APPLICATIONINSIGHTS_CONNECTION_STRING="$(az monitor app-insights component show \
  --app cloudsoftjob-appi \
  --resource-group cloudsoft-job-aca-rg \
  --query connectionString \
  -o tsv)"
```

Confirm it is set:

```bash
echo "$APPLICATIONINSIGHTS_CONNECTION_STRING"
```

Run the app from the same terminal:

```bash
dotnet run --project src/Cloudsoft.Web
```

If the Container App is configured manually instead of through Bicep, store the connection string as a secret and reference it as an environment variable:

```bash
az containerapp secret set \
  --name <container-app-name> \
  --resource-group <resource-group> \
  --secrets "appinsights-conn=<connection-string>"
```

```bash
az containerapp update \
  --name <container-app-name> \
  --resource-group <resource-group> \
  --set-env-vars "APPLICATIONINSIGHTS_CONNECTION_STRING=secretref:appinsights-conn"
```

Use both paths when possible:

- Container Apps console logs show what the app wrote and platform log flow.
- Application Insights adds request, dependency, exception, and trace correlation.

## Step 7: KQL Queries For Incidents

### Incident Query: Failed Login Spike

Use this when users report login problems or when you suspect credential probing.

```kql
ContainerAppConsoleLogs_CL
| where TimeGenerated > ago(1h)
| where Log_s has "Employer login failed"
   or Log_s has "Employer login rejected"
| summarize FailedLoginEvents = count() by bin(TimeGenerated, 5m), ContainerAppName_s, RevisionName_s
| order by TimeGenerated desc
```

![KQL output](../Images/KQL_query_and_output1.png)

### Incident Query: Job Create Or Update Failures

Use this when employers report that job creation or activation/deactivation is not working.

```kql
ContainerAppConsoleLogs_CL
| where TimeGenerated > ago(2h)
| where Log_s has "Job creation rejected"
   or Log_s has "Job status update failed"
| project TimeGenerated, ContainerAppName_s, RevisionName_s, Log_s
| order by TimeGenerated desc
```

![KQL output](../Images/KQL_query_and_output2.png)

### Incident Query: Repository Fallback Or Unexpected Errors

Use this when the application is running but data behavior looks wrong.

```kql
ContainerAppConsoleLogs_CL
| where TimeGenerated > ago(2h)
| where Log_s has "failed"
   or Log_s has "exception"
   or Log_s has "Falling back to in-memory storage"
| project TimeGenerated, ContainerAppName_s, RevisionName_s, Log_s
| order by TimeGenerated desc
```

![KQL output](../Images/KQL_query_and_output3.png)

### Application Insights Query: Slow Requests

Query after the Application Insights SDK is enabled.

```kql
requests
| where timestamp > ago(15h)
| where cloud_RoleName has "cloudsoft"
| summarize RequestCount = count(), AvgDuration = avg(duration), P95Duration = percentile(duration, 95) by name, bin(timestamp, 5m)
| order by P95Duration desc
```

![KQL output](../Images/KQL_query_and_output.png)

### Application Insights Query: Correlate One Failed Operation

Query after the Application Insights SDK is enabled with `operation_Id`.

```kql
union requests, traces, exceptions, dependencies
| where timestamp > ago(2h)
| where operation_Id == "<operation-id>"
| order by timestamp asc
```

## Step 8: Why Structured Logging Is Better Than Free Text

Structured logging keeps important values as named properties:

```csharp
_logger.LogInformation(
    "Job created. JobPostingId: {JobPostingId}, EmployerId: {EmployerId}, IsActive: {IsActive}",
    jobPosting.Id,
    jobPosting.EmployerId,
    jobPosting.IsActive);
```

This is better than:

```csharp
_logger.LogInformation($"Job {jobPosting.Id} was created by {jobPosting.EmployerId}");
```

Structured logs are better because:

- Queries can filter by property names such as `JobPostingId` or `EmployerId`.
- Dashboards can group by values without parsing strings.
- Alert rules can count specific event types.
- Log messages remain consistent across code changes.
- Sensitive values are easier to control because each logged value is intentional.

## Step 9: Verify Locally

Run the web app:

```bash
dotnet run --project src/Cloudsoft.Web
```

Exercise the flows:

1. Open the login page.
2. Try an invalid login.
3. Log in as an employer.
4. Create a job posting.
5. Activate or deactivate a job posting.
6. Watch the terminal output for structured log messages.

![Observability output](../Images/observability.png)
