# User Stories

This document describes the user stories supported by the CloudSoft Job Portal so far, followed by suggested improvement stories for future development.

## Current App Stories

### US-01: Browse Job Listings

**As a job seeker,** I want to browse available job postings without logging in, **so that** I can quickly see job opportunities.

**Acceptance criteria:**

- Job seekers can open the jobs page without an employer account.
- Each job listing shows the job title, location, description, and application deadline.
- When no job listings exist, the page shows a clear empty state.

### US-02: View Job Details

**As a job seeker,** I want to open a job posting, **so that** I can read the full job information before deciding whether to apply.

**Acceptance criteria:**

- A job seeker can select "View Details" from the job listing page.
- The details page shows the title, location, description, and deadline.
- If a job posting cannot be found, the app returns a not found response.

### US-03: Register Employer Account

**As an employer,** I want to register an account, **so that** I can post jobs on the portal.

**Acceptance criteria:**

- Employers can register with display name, email, password, and password confirmation.
- Registration validates required fields and matching passwords.
- Registration prevents duplicate employer emails.
- After successful registration, the employer is signed in automatically.

### US-04: Employer Login

**As an employer,** I want to log in with my email and password, **so that** I can access employer-only actions.

**Acceptance criteria:**

- Employers can log in from the employer login page.
- Invalid credentials show a validation error.
- After successful login, the employer is redirected to the requested local page or the jobs page.
- Logged-in employers see their display name in the navigation bar.

### US-05: Employer Logout

**As an employer,** I want to log out, **so that** other users cannot access my employer session on the same device.

**Acceptance criteria:**

- Logged-in employers can log out from the navigation bar.
- Logging out clears the authentication cookie.
- After logout, the employer is redirected to the jobs page.

### US-06: Post New Job

**As an employer,** I want to create a job posting, **so that** job seekers can see open roles from my company.

**Acceptance criteria:**

- Only authenticated employers can access the job posting form.
- The form captures title, description, location, deadline, and active status.
- Title, description, location, and deadline are required.
- The deadline cannot be in the past.
- After successful creation, the app redirects back to the job listing page and shows a success message.

### US-07: Protect Employer-Only Actions

**As the system,** I want employer-only actions to require authentication, **so that** unauthenticated users cannot post or manage jobs.

**Acceptance criteria:**

- Unauthenticated users are redirected to the login page when trying to access protected job actions.
- Return URLs are preserved for local app pages.
- Non-local return URLs are ignored to avoid unsafe redirects.

### US-08: Activate Or Deactivate Job Posting

**As an employer,** I want to toggle whether a job posting is active, **so that** I can control whether a role is currently open.

**Acceptance criteria:**

- Only authenticated employers can toggle a job posting's active status.
- The app updates the job posting's active state.
- The app shows a success message after the state changes.
- If the job posting does not exist, the app returns a not found response.

### US-09: Access Job Data Through API

**As an API consumer,** I want job posting endpoints, **so that** other clients or services can read and create job postings.

**Acceptance criteria:**

- The API can return all job postings.
- The API can return only active job postings.
- The API can return a single job posting by ID.
- The API can create a new job posting and return a created response.
- Missing job IDs return a not found response.

### US-10: Use Configurable Persistence

**As an operator,** I want the app to support both MongoDB and in-memory storage, **so that** it can run locally and in deployed environments.

**Acceptance criteria:**

- The app can use in-memory repositories when MongoDB is disabled or not configured.
- The app can use MongoDB repositories when MongoDB settings are configured.
- If MongoDB runtime operations fail, resilient repositories fall back to in-memory storage.
- Feature flags control MongoDB and Azure Key Vault configuration.

## Improvement Stories

### IMP-01: Search And Filter Jobs

**As a job seeker,** I want to search and filter job postings, **so that** I can find relevant jobs faster.

**Acceptance criteria:**

- Job seekers can search by title, description, or location.
- Job seekers can filter by active jobs.
- Job seekers can sort by newest posting or nearest deadline.
- Search and filter choices remain visible after results load.

**Priority:** High

### IMP-02: Apply For A Job

**As a job seeker,** I want to apply for a job from the job details page, **so that** I can submit my interest directly through the portal.

**Acceptance criteria:**

- The job details page has an apply action for active jobs.
- Applicants can submit name, email, message, and CV or profile link.
- The system validates required application fields.
- Employers can later review submitted applications.

**Priority:** High

### IMP-03: Employer Job Management Dashboard

**As an employer,** I want a dashboard of my own job postings, **so that** I can manage my company's roles in one place.

**Acceptance criteria:**

- Employers can see only the jobs they created.
- Employers can edit, activate, deactivate, and delete their own postings.
- Employers can see created date, deadline, and active status for each posting.
- The dashboard is protected by authentication.

**Priority:** High

### IMP-04: Store Job Ownership

**As the system,** I want each job posting connected to the employer who created it, **so that** employer permissions can be enforced correctly.

**Acceptance criteria:**

- Job postings store the employer user ID.
- New job postings automatically use the logged-in employer as owner.
- Employers cannot edit, deactivate, or delete jobs owned by another employer.
- API and MVC behavior follow the same ownership rules.

**Priority:** High

### IMP-05: Secure Password Storage

**As an employer,** I want my password stored securely, **so that** my account is protected if storage is exposed.

**Acceptance criteria:**

- Passwords are hashed before they are stored.
- Login compares submitted passwords against password hashes.
- Existing plaintext test or seed accounts are migrated or recreated safely.
- Password validation rules are documented and enforced.

**Priority:** High

### IMP-06: Improve Job Posting Validation

**As an employer,** I want clear validation when posting a job, **so that** I can fix mistakes before publishing.

**Acceptance criteria:**

- Title, location, and description have sensible length limits.
- Deadline validation works consistently across MVC and API creation.
- Validation errors are shown next to the relevant fields.
- API validation returns clear error responses.

**Priority:** Medium

### IMP-07: Public Active Jobs View

**As a job seeker,** I want the public jobs page to show only active postings, **so that** I do not spend time viewing closed jobs.

**Acceptance criteria:**

- The public jobs list excludes inactive jobs by default.
- Employers can still see inactive jobs in their dashboard.
- Direct links to inactive jobs show a clear closed-job status or are hidden from public access.

**Priority:** Medium

### IMP-08: Job Expiration

**As the system,** I want jobs to expire after their deadline, **so that** outdated jobs are not presented as open.

**Acceptance criteria:**

- Jobs past their deadline are treated as closed or inactive.
- Job seekers can clearly see when a job is closed.
- Employers can extend a deadline from their dashboard.
- API active-job responses exclude expired jobs.

**Priority:** Medium

### IMP-09: API Authentication And Authorization

**As an API owner,** I want protected API write endpoints, **so that** external clients cannot create or modify jobs without permission.

**Acceptance criteria:**

- API job creation requires authentication.
- API update, delete, and activation endpoints require authorization.
- Unauthorized API calls return appropriate HTTP status codes.
- API authentication is documented.

**Priority:** Medium

### IMP-10: Better API Documentation

**As a developer,** I want complete API documentation, **so that** I can integrate with the job portal reliably.

**Acceptance criteria:**

- `docs/api-spec.md` documents all API endpoints, request bodies, responses, and error cases.
- The API exposes Swagger/OpenAPI in development.
- Example requests and responses are included.

**Priority:** Medium

### IMP-11: Deployment Health And Monitoring

**As an operator,** I want health checks and monitoring, **so that** I can detect production issues quickly.

**Acceptance criteria:**

- The web app and API expose health endpoints.
- Health checks include storage connectivity when MongoDB is enabled.
- Deployment scripts verify health endpoints after deployment.
- Logs include enough detail to diagnose repository fallback events.

**Priority:** Medium

### IMP-12: Employer Profile

**As an employer,** I want to manage my company profile, **so that** job seekers can understand who is hiring.

**Acceptance criteria:**

- Employers can edit company name, description, website, and contact email.
- Job details show employer profile information.
- Employer profile changes apply to future and existing job postings.

**Priority:** Low

### IMP-13: Saved Jobs

**As a job seeker,** I want to save interesting jobs, **so that** I can return to them later.

**Acceptance criteria:**

- Job seekers can save and unsave jobs.
- Saved jobs are available from a dedicated page.
- Saved jobs remain available across sessions for signed-in job seekers.

**Priority:** Low

### IMP-14: Email Notifications

**As an employer or applicant,** I want email notifications, **so that** important application activity is not missed.

**Acceptance criteria:**

- Employers receive an email when a new application is submitted.
- Applicants receive a confirmation email after applying.
- Email sending failures are logged without breaking the application flow.
- Email templates are configurable.

**Priority:** Low
