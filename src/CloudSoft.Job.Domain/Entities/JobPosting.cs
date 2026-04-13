/* // Defines the namespace this entity belongs to in the domain project.
namespace CloudSoft.Job.Domain.Entities;

// Represents a job posting in the CloudSoft job domain.
public class JobPosting
{
    // Unique identifier for the job posting; a new GUID string is created by default.
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // Job title, such as "Software Engineer"; defaults to an empty string.
    public string Title { get; set; } = string.Empty;

    // Full job description; defaults to an empty string.
    public string Description { get; set; } = string.Empty;

    // Job location, such as a city, country, or remote label; defaults to an empty string.
    public string Location { get; set; } = string.Empty;

    // UTC timestamp for when the job posting is created; defaults to the current UTC time.
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Shows whether the job posting is currently active; defaults to active.
    public bool IsActive { get; set; } = true;
}
 */

using System.ComponentModel.DataAnnotations;

namespace CloudSoft.Job.Domain.Entities;

public class JobPosting
{
    [Required]
    public string? Title { get; set; }

    [Required]
    public string? Description { get; set; }

    [Required]
    public string? Location { get; set; }
}