using Cloudsoft.Core.Data;
using Cloudsoft.Core.Repositories;
using Cloudsoft.Core.Services;

namespace Cloudsoft.Tests.Unit;

public class EmployerAuthenticationServiceTests
{
    private readonly EmployerAuthenticationService _service = new(new EmployerRepository(new InMemoryDatabase()));

    [Fact]
    public async Task RegisterAsync_CreatesEmployerAccount()
    {
        var employer = await _service.RegisterAsync(" hiring@example.com ", "Password123!", " Example AB ");

        Assert.NotNull(employer);
        Assert.False(string.IsNullOrWhiteSpace(employer.Id));
        Assert.Equal("hiring@example.com", employer.Email);
        Assert.Equal("Example AB", employer.DisplayName);
    }

    [Theory]
    [InlineData("", "Password123!", "Example AB")]
    [InlineData("hiring@example.com", "", "Example AB")]
    [InlineData("hiring@example.com", "Password123!", "")]
    public async Task RegisterAsync_ReturnsNullWhenRequiredValuesAreMissing(
        string email,
        string password,
        string displayName)
    {
        var employer = await _service.RegisterAsync(email, password, displayName);

        Assert.Null(employer);
    }

    [Fact]
    public async Task RegisterAsync_PreventsDuplicateEmployerEmails()
    {
        var first = await _service.RegisterAsync("dupe@example.com", "Password123!", "First");
        var second = await _service.RegisterAsync(" DUPE@example.com ", "Password123!", "Second");

        Assert.NotNull(first);
        Assert.Null(second);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ReturnsEmployerForValidCredentials()
    {
        await _service.RegisterAsync("login@example.com", "Password123!", "Login AB");

        var employer = await _service.ValidateCredentialsAsync(" LOGIN@example.com ", "Password123!");

        Assert.NotNull(employer);
        Assert.Equal("Login AB", employer.DisplayName);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ReturnsNullForInvalidCredentials()
    {
        await _service.RegisterAsync("login@example.com", "Password123!", "Login AB");

        var employer = await _service.ValidateCredentialsAsync("login@example.com", "WrongPassword");

        Assert.Null(employer);
    }
}
