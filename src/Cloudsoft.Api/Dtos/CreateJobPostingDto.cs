using System.ComponentModel.DataAnnotations;

namespace Cloudsoft.Api.Dtos;

public class CreateJobPostingDto
{
    [Required(ErrorMessage = "The job title is required.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "A job description is required.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "The job location is required.")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "The application deadline is required.")]
    [DataType(DataType.Date)]
    public DateTime Deadline { get; set; }

    public bool IsActive { get; set; } = true;
}