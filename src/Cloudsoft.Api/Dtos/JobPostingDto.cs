namespace Cloudsoft.Api.Dtos;

public class JobPostingDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime Deadline { get; set; }
    public bool IsActive { get; set; }
}