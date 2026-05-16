using System.Collections.Concurrent;
using Cloudsoft.Core.Models;

namespace Cloudsoft.Core.Data;

public class InMemoryDatabase : IInMemoryDatabase
{
    public InMemoryDatabase()
    {
        var demoEmployer = new EmployerAccount
        {
            User = new EmployerUser
            {
                Id = "cloudsoft-employer",
                Email = "employer@cloudsoft.com",
                DisplayName = "CloudSoft Employer"
            },
            Password = "Password123!",
            NormalizedEmail = NormalizeEmail("employer@cloudsoft.com")
        };

        Employers.TryAdd(NormalizeEmail(demoEmployer.User.Email), demoEmployer);
    }

    public ConcurrentDictionary<string, JobPosting> JobPostings { get; } = new();

    public ConcurrentDictionary<string, EmployerAccount> Employers { get; } = new();

    public ConcurrentDictionary<string, JobApplication> JobApplications { get; } = new();

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }
}
