# Cookie Authentication Guide

This document explains how cookie authentication works in the CloudSoft Job Portal application. The authentication flow is used for employer login, employer registration, protected job-posting actions, and logout.

Target flow:

```text
Anonymous visitor
-> Opens /Account/Login or /Account/Register
-> Submits employer credentials
-> AccountController validates or creates the employer account
-> SignInAsync writes the authentication cookie
-> Browser is redirected to the requested local URL or /Job
-> Protected job actions read User.Identity from the cookie principal
```

Files involved:

- `src/Cloudsoft.Web/Program.cs`
- `src/Cloudsoft.Web/Controllers/AccountController.cs`
- `src/Cloudsoft.Web/Controllers/JobsController.cs`
- `src/Cloudsoft.Web/Models/LoginViewModel.cs`
- `src/Cloudsoft.Web/Models/RegisterViewModel.cs`
- `src/Cloudsoft.Web/Views/Account/Login.cshtml`
- `src/Cloudsoft.Web/Views/Account/Register.cshtml`
- `src/Cloudsoft.Web/Views/Shared/_Layout.cshtml`
- `src/Cloudsoft.Core/Services/EmployerAuthenticationService.cs`
- `src/Cloudsoft.Core/Repositories/EmployerRepository.cs`
- `src/Cloudsoft.Core/Data/InMemory/InMemoryDatabase.cs`

## 1. Register Authentication Services

Cookie authentication is configured in `src/Cloudsoft.Web/Program.cs`.

```csharp
using Microsoft.AspNetCore.Authentication.Cookies;

builder.Services.AddScoped<IEmployerAuthenticationService, EmployerAuthenticationService>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
    });
```

What this does:

1. `IEmployerAuthenticationService` is registered so `AccountController` can validate and register employers.
2. `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)` sets cookie authentication as the default scheme.
3. `AddCookie(...)` registers ASP.NET Core's cookie authentication handler.
4. `LoginPath = "/Account/Login"` redirects anonymous users to the employer login page when they access a protected endpoint.
5. `AccessDeniedPath = "/Account/Login"` sends denied users back to the login page.

## 2. Register Employer Storage

The authentication service depends on `IEmployerRepository`. `Program.cs` chooses the repository implementation based on the MongoDB feature flag and configuration.

```csharp
builder.Services.AddSingleton<IInMemoryDatabase, InMemoryDatabase>();

var useMongoDb = featureFlags.UseMongoDb && HasValidMongoConfiguration(builder.Configuration);

if (useMongoDb)
{
    builder.Services.AddScoped<MongoEmployerRepository>();
    builder.Services.AddScoped<EmployerRepository>();
    builder.Services.AddScoped<IEmployerRepository, ResilientEmployerRepository>();
}
else
{
    builder.Services.AddScoped<IEmployerRepository, EmployerRepository>();
}
```

What this means:

1. In local or fallback mode, employer accounts are stored in the in-memory database.
2. When MongoDB is enabled and configured, `ResilientEmployerRepository` uses MongoDB with in-memory fallback behavior.
3. `EmployerAuthenticationService` does not need to know which storage implementation is active.

## 3. Add Authentication Middleware

The request pipeline in `Program.cs` enables authentication before authorization.

```csharp
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
```

The order matters:

1. `UseRouting()` selects the endpoint.
2. `UseAuthentication()` reads the cookie and builds `HttpContext.User`.
3. `UseAuthorization()` checks `[Authorize]` requirements.
4. `MapControllerRoute(...)` maps MVC controller actions.

`UseAuthentication()` must run before `UseAuthorization()`.

## 4. Define Login Data

The login form uses `src/Cloudsoft.Web/Models/LoginViewModel.cs`.

```csharp
using System.ComponentModel.DataAnnotations;

namespace Cloudsoft.Web.Models;

public class LoginViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
```

Important details:

1. `Email` is required and must be a valid email address.
2. `Password` is required and rendered as a password input.
3. `ReturnUrl` preserves the protected URL the employer originally tried to access.

## 5. Render The Employer Login Form

The login page is `src/Cloudsoft.Web/Views/Account/Login.cshtml`.

```cshtml
@model LoginViewModel

@{
    ViewData["Title"] = "Employer Login";
}

<form asp-controller="Account" asp-action="Login" method="post">
    <input asp-for="ReturnUrl" type="hidden" />

    <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>

    <div class="mb-3">
        <label asp-for="Email" class="form-label"></label>
        <input asp-for="Email" class="form-control" autocomplete="username" />
        <span asp-validation-for="Email" class="text-danger small"></span>
    </div>

    <div class="mb-3">
        <label asp-for="Password" class="form-label"></label>
        <input asp-for="Password" class="form-control" autocomplete="current-password" />
        <span asp-validation-for="Password" class="text-danger small"></span>
    </div>

    <button type="submit" class="btn btn-primary w-100">
        <i class="fas fa-right-to-bracket me-2"></i>Login
    </button>
</form>
```

Important details:

1. The form posts to `AccountController.Login(LoginViewModel model)`.
2. The `ReturnUrl` hidden input keeps the original destination.
3. Validation messages are displayed using ASP.NET Core tag helpers.
4. The form tag helper emits the antiforgery token for the POST action.

## 6. Validate Login And Create The Cookie

The login actions are in `src/Cloudsoft.Web/Controllers/AccountController.cs`.

```csharp
[HttpGet("Login")]
public IActionResult Login(string? returnUrl = null)
{
    if (User.Identity?.IsAuthenticated == true)
    {
        return RedirectToLocal(returnUrl);
    }

    return View(new LoginViewModel { ReturnUrl = returnUrl });
}

[HttpPost("Login")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Login(LoginViewModel model)
{
    if (!ModelState.IsValid)
    {
        return View(model);
    }

    var employer = await _employerAuthenticationService.ValidateCredentialsAsync(model.Email, model.Password);
    if (employer == null)
    {
        ModelState.AddModelError(string.Empty, "Invalid employer login.");
        return View(model);
    }

    await SignInEmployerAsync(employer);

    return RedirectToLocal(model.ReturnUrl);
}
```

Step by step:

1. The GET action returns the login page unless the current user is already authenticated.
2. The POST action validates model binding and data annotations.
3. `_employerAuthenticationService.ValidateCredentialsAsync(...)` checks the email and password.
4. Invalid credentials return the login page with `Invalid employer login.`
5. Valid credentials call `SignInEmployerAsync(...)`.
6. The user is redirected to a local `ReturnUrl` or to the jobs page.

## 7. Validate Employer Credentials

`src/Cloudsoft.Core/Services/EmployerAuthenticationService.cs` checks credentials through the employer repository.

```csharp
public async Task<EmployerUser?> ValidateCredentialsAsync(string email, string password)
{
    var employerAccount = await _employerRepository.GetByEmailAsync(email);
    if (employerAccount == null || employerAccount.Password != password)
    {
        return null;
    }

    return employerAccount.User;
}
```

The in-memory repository normalizes email addresses before lookup.

```csharp
private static string NormalizeEmail(string email)
{
    return email.Trim().ToUpperInvariant();
}
```

Security note:

1. The current implementation compares plain text passwords.
2. This is acceptable only for a learning or prototype app.
3. A production implementation should hash passwords with a proven password hasher and never store raw passwords.

## 8. Create Employer Claims

`AccountController.SignInEmployerAsync(...)` creates the authenticated employer identity.

```csharp
private async Task SignInEmployerAsync(EmployerUser employer)
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, employer.Id),
        new(ClaimTypes.Name, employer.DisplayName),
        new(ClaimTypes.Email, employer.Email),
        new(ClaimTypes.Role, "Employer")
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
}
```

Claims created:

1. `ClaimTypes.NameIdentifier` stores the employer user ID.
2. `ClaimTypes.Name` stores the employer display name.
3. `ClaimTypes.Email` stores the employer email address.
4. `ClaimTypes.Role` stores the `Employer` role.

`SignInAsync(...)` serializes this principal into an encrypted authentication cookie.

## 9. Register A New Employer

The registration form uses `src/Cloudsoft.Web/Models/RegisterViewModel.cs`.

```csharp
public class RegisterViewModel
{
    [Required]
    [Display(Name = "Company or display name")]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
```

Registration is handled by `AccountController.Register(...)`.

```csharp
[HttpPost("Register")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Register(RegisterViewModel model)
{
    if (!ModelState.IsValid)
    {
        return View(model);
    }

    if (await _employerAuthenticationService.EmailExistsAsync(model.Email))
    {
        ModelState.AddModelError(nameof(model.Email), "An employer account already exists for this email.");
        return View(model);
    }

    var employer = await _employerAuthenticationService.RegisterAsync(model.Email, model.Password, model.DisplayName);
    if (employer == null)
    {
        ModelState.AddModelError(string.Empty, "Unable to register employer account.");
        return View(model);
    }

    await SignInEmployerAsync(employer);

    return RedirectToLocal(model.ReturnUrl);
}
```

Step by step:

1. The form validates display name, email, password, and password confirmation.
2. Existing employer email addresses are rejected.
3. A new employer account is created through `EmployerAuthenticationService`.
4. The new employer is signed in immediately.
5. The user is redirected to the original local URL or to the jobs page.

## 10. Protect Job Posting Actions

Protected employer actions are in `src/Cloudsoft.Web/Controllers/JobsController.cs`.

```csharp
[Authorize]
[HttpGet("Create")]
public IActionResult Create()
{
    return View("JobPosting", new JobPosting());
}

[Authorize]
[HttpPost("Create")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(JobPosting jobPosting)
{
    if (jobPosting.Deadline == default)
    {
        ModelState.AddModelError(nameof(jobPosting.Deadline), "The application deadline is required.");
    }

    if (jobPosting.Deadline.Date < DateTime.UtcNow.Date)
    {
        ModelState.AddModelError(nameof(jobPosting.Deadline), "The application deadline cannot be in the past.");
    }

    if (!ModelState.IsValid)
    {
        return View("JobPosting", jobPosting);
    }

    await _jobPostingService.CreateAsync(jobPosting);
    TempData["SuccessMessage"] = $"Thank you for posting the {jobPosting.Title} job!";

    return RedirectToAction(nameof(Index));
}
```

What happens when an anonymous visitor opens `/Job/Create`:

1. `[Authorize]` requires an authenticated user.
2. Cookie authentication sees that no valid auth cookie exists.
3. The visitor is redirected to `/Account/Login`.
4. After login or registration, `RedirectToLocal(...)` can send the user back to the original local URL.

`ToggleIsActive` is also protected with `[Authorize]`, so only authenticated employers can activate or deactivate job postings.

## 11. Show Login And Logout In The Layout

The shared navigation in `src/Cloudsoft.Web/Views/Shared/_Layout.cshtml` uses `User.Identity`.

```cshtml
@if (User.Identity?.IsAuthenticated == true)
{
    <li class="nav-item">
        <span class="navbar-text text-light me-3">@User.Identity.Name</span>
    </li>
    <li class="nav-item">
        <form asp-controller="Account" asp-action="Logout" method="post" class="d-inline">
            <button type="submit" class="btn btn-link nav-link p-0">Logout</button>
        </form>
    </li>
}
else
{
    <li class="nav-item">
        <a class="nav-link" asp-controller="Account" asp-action="Login">Employer Login</a>
    </li>
}
```

When the user is anonymous:

1. The navigation shows `Employer Login`.
2. Protected job actions redirect to `/Account/Login`.

When the employer is signed in:

1. The navigation shows the employer display name from `ClaimTypes.Name`.
2. The navigation shows a `Logout` form.

## 12. Log Out

Logout is handled by `AccountController.Logout()`.

```csharp
[HttpPost("Logout")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Logout()
{
    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return RedirectToAction("Index", "Jobs");
}
```

Step by step:

1. The user submits the logout form from the layout.
2. MVC calls `POST /Account/Logout`.
3. `[ValidateAntiForgeryToken]` validates the request.
4. `SignOutAsync(...)` removes the authentication cookie.
5. The user is redirected to the jobs page.
6. Future requests are anonymous again.

## 13. Complete Flow

Login:

1. The employer opens `/Account/Login`.
2. The login form renders with `LoginViewModel`.
3. The employer submits email and password.
4. `EmployerAuthenticationService.ValidateCredentialsAsync(...)` checks the account.
5. `SignInEmployerAsync(...)` creates employer claims.
6. `SignInAsync(...)` writes the authentication cookie.
7. The employer is redirected to the requested local URL or `/Job`.

Registration:

1. The employer opens `/Account/Register`.
2. The registration form renders with `RegisterViewModel`.
3. The employer submits display name, email, password, and password confirmation.
4. `EmailExistsAsync(...)` prevents duplicate employer accounts.
5. `RegisterAsync(...)` creates the employer account.
6. `SignInEmployerAsync(...)` signs in the new employer.
7. The employer is redirected to the requested local URL or `/Job`.

Protected job action:

1. An anonymous user opens `/Job/Create`.
2. `[Authorize]` triggers the cookie authentication challenge.
3. The browser is redirected to `/Account/Login`.
4. After successful login, the app redirects to a local return URL when one is provided.
5. `UseAuthentication()` rebuilds `HttpContext.User` on later requests.
6. `JobsController` can execute the protected action.

Logout:

1. The employer submits the logout form.
2. `SignOutAsync(...)` removes the cookie.
3. The user is redirected to `/Job`.
4. The layout shows `Employer Login` again.
