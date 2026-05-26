using System.Security.Claims;
using Cloudsoft.Core.Models;
using Cloudsoft.Core.Services.Interfaces;
using Cloudsoft.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Cloudsoft.Web.Controllers;

[Route("Account")]
public class AccountController : Controller
{
    private readonly IEmployerAuthenticationService _employerAuthenticationService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IEmployerAuthenticationService employerAuthenticationService,
        ILogger<AccountController> logger)
    {
        _employerAuthenticationService = employerAuthenticationService;
        _logger = logger;
    }

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
            _logger.LogWarning(
                "Employer login rejected because the submitted model is invalid. ValidationErrorCount: {ValidationErrorCount}",
                ModelState.ErrorCount);

            return View(model);
        }

        var employer = await _employerAuthenticationService.ValidateCredentialsAsync(model.Email, model.Password);
        if (employer == null)
        {
            _logger.LogWarning("Employer login failed. Reason: {Reason}", "InvalidCredentials");

            ModelState.AddModelError(string.Empty, "Invalid employer login.");
            return View(model);
        }

        await SignInEmployerAsync(employer);
        _logger.LogInformation("Employer login succeeded. EmployerId: {EmployerId}", employer.Id);

        return RedirectToLocal(model.ReturnUrl);
    }

    [HttpGet("Register")]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

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

    [HttpPost("Logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var employerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("Employer logged out. EmployerId: {EmployerId}", employerId);

        return RedirectToAction("Index", "Jobs");
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Jobs");
    }

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
}
