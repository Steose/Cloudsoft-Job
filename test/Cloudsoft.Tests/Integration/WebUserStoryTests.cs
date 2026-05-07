using System.Net;
using System.Net.Http.Headers;
using Cloudsoft.Core.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Cloudsoft.Core.Services.Interfaces;

namespace Cloudsoft.Tests.Integration;

public class WebUserStoryTests
{
    [Fact]
    public async Task JobListingPage_IsAvailableToAnonymousUsersAndShowsEmptyState()
    {
        await using var factory = new CloudsoftWebFactory();
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/Job");

        Assert.Contains("Job Listings", html);
        Assert.Contains("No job listings available.", html);
        Assert.Contains("Register as Employer to Post Job", html);
    }

    [Fact]
    public async Task JobDetailsPage_ShowsFullJobInformation()
    {
        await using var factory = new CloudsoftWebFactory();
        var job = await SeedJobAsync(factory, "web-details");
        var client = factory.CreateClient();

        var html = await client.GetStringAsync($"/Job/Details/{job.Id}");

        Assert.Contains(job.Title, html);
        Assert.Contains(job.Location, html);
        Assert.Contains(job.Description, html);
        Assert.Contains(job.Deadline.ToString("yyyy-MM-dd"), html);
    }

    [Fact]
    public async Task JobDetailsPage_ReturnsNotFoundForMissingJobPosting()
    {
        await using var factory = new CloudsoftWebFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/Job/Details/missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedCreatePage_RedirectsAnonymousUsersToLoginWithReturnUrl()
    {
        await using var factory = new CloudsoftWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Job/Create");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/Account/Login", response.Headers.Location.ToString());
        Assert.Contains("ReturnUrl=%2FJob%2FCreate", response.Headers.Location.ToString());
    }

    [Fact]
    public async Task RegisterEmployerAccount_SignsInAndRedirectsToLocalReturnUrl()
    {
        await using var factory = new CloudsoftWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var token = await AntiforgeryToken.ReadAsync(client, "/Account/Register?returnUrl=/Job");
        var email = $"new-employer-{Guid.NewGuid():N}@example.com";

        var response = await client.PostAsync("/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["DisplayName"] = "New Employer",
            ["Email"] = email,
            ["Password"] = "Password123!",
            ["ConfirmPassword"] = "Password123!",
            ["ReturnUrl"] = "/Job",
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Job", response.Headers.Location?.ToString());
        Assert.Contains(response.Headers, header => header.Key == "Set-Cookie");
    }

    [Fact]
    public async Task LoginEmployerAccount_ShowsDisplayNameInNavigation()
    {
        await using var factory = new CloudsoftWebFactory();
        var client = factory.CreateClient();

        await LoginSeedEmployerAsync(client);
        var html = await client.GetStringAsync("/Job");

        Assert.Contains("CloudSoft Employer", html);
        Assert.Contains("Logout", html);
    }

    [Fact]
    public async Task InvalidLogin_ShowsValidationError()
    {
        await using var factory = new CloudsoftWebFactory();
        var client = factory.CreateClient();
        var token = await AntiforgeryToken.ReadAsync(client, "/Account/Login");

        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "employer@cloudsoft.com",
            ["Password"] = "WrongPassword",
            ["__RequestVerificationToken"] = token
        }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Invalid employer login.", html);
    }

    [Fact]
    public async Task LoginEmployerAccount_IgnoresNonLocalReturnUrl()
    {
        await using var factory = new CloudsoftWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var token = await AntiforgeryToken.ReadAsync(client, "/Account/Login");

        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "employer@cloudsoft.com",
            ["Password"] = "Password123!",
            ["ReturnUrl"] = "https://evil.example/phish",
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Job", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Logout_ClearsSessionAndRedirectsToJobsPage()
    {
        await using var factory = new CloudsoftWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginSeedEmployerAsync(client);
        var token = await AntiforgeryToken.ReadAsync(client, "/Job");

        var response = await client.PostAsync("/Account/Logout", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Job", response.Headers.Location?.ToString());
        Assert.Contains(response.Headers, header =>
            header.Key == "Set-Cookie" &&
            header.Value.Any(value => value.Contains(".AspNetCore.Cookies") && value.Contains("expires=", StringComparison.OrdinalIgnoreCase)));
    }

    private static async Task LoginSeedEmployerAsync(HttpClient client)
    {
        var token = await AntiforgeryToken.ReadAsync(client, "/Account/Login");
        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "employer@cloudsoft.com",
            ["Password"] = "Password123!",
            ["__RequestVerificationToken"] = token
        }));

        Assert.True(
            response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Redirect,
            $"Login returned unexpected status code {response.StatusCode}.");
    }

    private static async Task<JobPosting> SeedJobAsync(CloudsoftWebFactory factory, string id)
    {
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IJobPostingService>();
        return await service.CreateAsync(new JobPosting
        {
            Id = $"{id}-{Guid.NewGuid():N}",
            Title = "Cloud Support Engineer",
            Description = "Help customers run production systems",
            Location = "Gothenburg",
            CreatedAtUtc = DateTime.UtcNow,
            Deadline = DateTime.UtcNow.AddDays(25),
            IsActive = true
        });
    }
}
