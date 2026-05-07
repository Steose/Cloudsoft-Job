using System.ComponentModel.DataAnnotations;
using Cloudsoft.Core.Models;
using Cloudsoft.Web.Models;

namespace Cloudsoft.Tests.Unit;

public class ViewModelValidationTests
{
    [Fact]
    public void RegisterViewModel_RequiresMatchingPasswords()
    {
        var model = new RegisterViewModel
        {
            DisplayName = "Example AB",
            Email = "hiring@example.com",
            Password = "Password123!",
            ConfirmPassword = "Different123!"
        };

        var results = Validate(model);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(RegisterViewModel.ConfirmPassword)));
    }

    [Fact]
    public void RegisterViewModel_RequiresEmailPasswordAndDisplayName()
    {
        var model = new RegisterViewModel();

        var results = Validate(model);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(RegisterViewModel.DisplayName)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(RegisterViewModel.Email)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(RegisterViewModel.Password)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(RegisterViewModel.ConfirmPassword)));
    }

    [Fact]
    public void LoginViewModel_RequiresEmailAndPassword()
    {
        var model = new LoginViewModel();

        var results = Validate(model);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(LoginViewModel.Email)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(LoginViewModel.Password)));
    }

    [Fact]
    public void JobPosting_RequiresTitleDescriptionLocationAndDeadline()
    {
        var model = new JobPosting();

        var results = Validate(model);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(JobPosting.Title)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(JobPosting.Description)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(JobPosting.Location)));
    }

    private static List<ValidationResult> Validate(object model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }
}
