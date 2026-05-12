using System.Security.Claims;
using System.Text.Encodings.Web;
using Cloudsoft.Core.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Cloudsoft.Api.Authentication;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ApiAuthOptions _apiAuthOptions;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<ApiAuthOptions> apiAuthOptions)
        : base(options, logger, encoder)
    {
        _apiAuthOptions = apiAuthOptions.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrWhiteSpace(_apiAuthOptions.WriteApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("API write key is not configured."));
        }

        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationDefaults.HeaderName, out var apiKeyValues))
        {
            return Task.FromResult(AuthenticateResult.Fail("API key is missing."));
        }

        var apiKey = apiKeyValues.FirstOrDefault();
        if (!string.Equals(apiKey, _apiAuthOptions.WriteApiKey, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("API key is invalid."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "api-client"),
            new Claim(ClaimTypes.Name, "API Client")
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
