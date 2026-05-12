# Cloudsoft API Spec

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

## Job Endpoints

### Get all jobs

```http
GET /api/jobs
```

Returns all job postings.

### Get active jobs

```http
GET /api/jobs/active
```

Returns only active job postings.

### Get job by ID

```http
GET /api/jobs/{id}
```

Returns `404 Not Found` when the job does not exist.

### Create job

```http
POST /api/jobs
X-API-Key: <configured-write-api-key>
Content-Type: application/json
```

Creates a job posting and returns `201 Created`.

Missing or invalid API keys return `401 Unauthorized`.
