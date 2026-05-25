# Cloudsoft API Spec

## Overview

The Cloudsoft API exposes job posting data from `Cloudsoft.Api`.

The API uses DTOs as its public contract:

- `CreateJobPostingDto` is used when creating a job posting.
- `JobPostingDto` is returned from job endpoints.
- `JobPosting` remains the internal domain model in `Cloudsoft.Core`.

This keeps the public API contract separate from the internal domain model.

## Swagger

Swagger is enabled with `Swashbuckle.AspNetCore`.

Run the API:

```bash
dotnet run --project src/Cloudsoft.Api
```

Open Swagger UI:

```text
http://localhost:5155/swagger
```

The HTTPS profile also exposes:

```text
https://localhost:7036/swagger
```

The OpenAPI JSON document is available at:

```text
http://localhost:5155/swagger/v1/swagger.json
```

## Authentication

Read endpoints are public.

Write endpoints require an API key in this header:

```http
X-API-Key: <configured-write-api-key>
```

The key is configured with:

```text
ApiAuth__WriteApiKey=<secret-value>
```

Do not commit production API keys to the repository.

Missing or invalid API keys return:

```http
401 Unauthorized
```

### Configure API Key Locally

For local development, configure the write API key with an environment variable:

```bash
export ApiAuth__WriteApiKey="test-api-write-key"
```

Then run the API from the same terminal:

```bash
dotnet run --project src/Cloudsoft.Api
```

Protected endpoints must include the configured key in the `X-API-Key` header.

Example:

```bash
curl -X POST http://localhost:5155/api/jobs \
  -H "Content-Type: application/json" \
  -H "X-API-Key: test-api-write-key" \
  -d '{
    "title": "Software Developer",
    "description": "Build and maintain web applications.",
    "location": "Stockholm",
    "deadline": "2026-06-30",
    "isActive": true
  }'
```

### Missing API Key Log

This log means a protected endpoint was called without the `X-API-Key` header:

```text
Cloudsoft.Api.Authentication.ApiKeyAuthenticationHandler[7]
      ApiKey was not authenticated. Failure message: API key is missing.
```

Fix it by sending this header with protected requests:

```http
X-API-Key: test-api-write-key
```

Public `GET` endpoints do not need an API key. `POST /api/jobs` does need an API key.

### Swagger API Key Support

Swagger UI can show an `Authorize` button for the API key if Swagger security is configured.

In `Program.cs`, replace:

```csharp
builder.Services.AddSwaggerGen();
```

with:

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "API key needed to access protected endpoints. Use: X-API-Key: <key>",
        Name = "X-API-Key",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "ApiKey"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});
```

Optionally add this using at the top of `Program.cs` and shorten the type names:

```csharp
using Microsoft.OpenApi.Models;
```

After restarting the API, open Swagger:

```text
http://localhost:5155/swagger
```

Click `Authorize` and enter only the key value:

```text
test-api-write-key
```

Do not enter `X-API-Key: test-api-write-key` in the Swagger authorize box.

## DTOs

### CreateJobPostingDto

Used as the request body for `POST /api/jobs`.

```json
{
  "title": "Software Developer",
  "description": "Build and maintain web applications.",
  "location": "Stockholm",
  "deadline": "2026-06-30",
  "isActive": true
}
```

Fields:

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `title` | string | Yes | Job title. |
| `description` | string | Yes | Job description. |
| `location` | string | Yes | Job location. |
| `deadline` | date/time | Yes | Final application date. |
| `isActive` | boolean | No | Defaults to `true`. |

The client does not send `id`, `createdAtUtc`, or `employerId`.

### JobPostingDto

Returned from job endpoints.

```json
{
  "id": "8f6f2c7e-4d4e-4c46-90f8-99bb2b86cb91",
  "title": "Software Developer",
  "description": "Build and maintain web applications.",
  "location": "Stockholm",
  "createdAtUtc": "2026-05-25T14:00:00Z",
  "deadline": "2026-06-30T00:00:00Z",
  "isActive": true
}
```

Fields:

| Field | Type | Notes |
| --- | --- | --- |
| `id` | string | Server-generated job posting ID. |
| `title` | string | Job title. |
| `description` | string | Job description. |
| `location` | string | Job location. |
| `createdAtUtc` | date/time | UTC timestamp when the posting was created. |
| `deadline` | date/time | Final application date. |
| `isActive` | boolean | Whether the posting is active. |

`employerId` is not exposed in the API response DTO.

## Job Endpoints

### Get All Jobs

```http
GET /api/jobs
```

Returns all job postings.

Response:

```http
200 OK
Content-Type: application/json
```

```json
[
  {
    "id": "8f6f2c7e-4d4e-4c46-90f8-99bb2b86cb91",
    "title": "Software Developer",
    "description": "Build and maintain web applications.",
    "location": "Stockholm",
    "createdAtUtc": "2026-05-25T14:00:00Z",
    "deadline": "2026-06-30T00:00:00Z",
    "isActive": true
  }
]
```

### Get Active Jobs

```http
GET /api/jobs/active
```

Returns only active job postings where `isActive` is `true`.

Response:

```http
200 OK
Content-Type: application/json
```

```json
[
  {
    "id": "8f6f2c7e-4d4e-4c46-90f8-99bb2b86cb91",
    "title": "Software Developer",
    "description": "Build and maintain web applications.",
    "location": "Stockholm",
    "createdAtUtc": "2026-05-25T14:00:00Z",
    "deadline": "2026-06-30T00:00:00Z",
    "isActive": true
  }
]
```

### Get Job By ID

```http
GET /api/jobs/{id}
```

Returns one job posting.

Successful response:

```http
200 OK
Content-Type: application/json
```

```json
{
  "id": "8f6f2c7e-4d4e-4c46-90f8-99bb2b86cb91",
  "title": "Software Developer",
  "description": "Build and maintain web applications.",
  "location": "Stockholm",
  "createdAtUtc": "2026-05-25T14:00:00Z",
  "deadline": "2026-06-30T00:00:00Z",
  "isActive": true
}
```

When the job posting does not exist:

```http
404 Not Found
```

### Create Job

```http
POST /api/jobs
X-API-Key: <configured-write-api-key>
Content-Type: application/json
```

Request body:

```json
{
  "title": "Software Developer",
  "description": "Build and maintain web applications.",
  "location": "Stockholm",
  "deadline": "2026-06-30",
  "isActive": true
}
```

Successful response:

```http
201 Created
Location: /api/jobs/{id}
Content-Type: application/json
```

```json
{
  "id": "8f6f2c7e-4d4e-4c46-90f8-99bb2b86cb91",
  "title": "Software Developer",
  "description": "Build and maintain web applications.",
  "location": "Stockholm",
  "createdAtUtc": "2026-05-25T14:00:00Z",
  "deadline": "2026-06-30T00:00:00Z",
  "isActive": true
}
```

The server generates the `id` and `createdAtUtc` values.

Validation errors return:

```http
400 Bad Request
```

Missing or invalid API keys return:

```http
401 Unauthorized
```

## Implementation Notes

The API controller maps between DTOs and the domain model:

```csharp
public static JobPostingDto ToDto(this JobPosting jobPosting)
```

```csharp
public static JobPosting ToModel(this CreateJobPostingDto dto)
```

The controller methods use DTOs:

```csharp
public async Task<IReadOnlyCollection<JobPostingDto>> GetAll()
public async Task<IReadOnlyCollection<JobPostingDto>> GetActive()
public async Task<ActionResult<JobPostingDto>> GetById(string id)
public async Task<ActionResult<JobPostingDto>> Create(CreateJobPostingDto dto)
```

## Verification

Run the tests:

```bash
dotnet test
```

Current verified result:

```text
Passed! - Failed: 0, Passed: 55, Skipped: 0, Total: 55
```
