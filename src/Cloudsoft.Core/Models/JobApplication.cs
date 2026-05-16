using System.ComponentModel.DataAnnotations;

namespace Cloudsoft.Core.Models;

public class JobApplication
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string JobPostingId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Your full name is required.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Your email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Select your country.")]
    public string CountryCode { get; set; } = string.Empty;

    public string CvFileName { get; set; } = string.Empty;

    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
}
