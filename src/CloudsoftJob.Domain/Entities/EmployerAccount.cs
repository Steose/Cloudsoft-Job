namespace CloudsoftJob.Core.Models;

public class EmployerAccount
{
    public EmployerUser User { get; set; } = new();

    public string Password { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;
}
