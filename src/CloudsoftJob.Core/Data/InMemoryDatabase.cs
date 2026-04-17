using System.Collections.Concurrent;
using CloudsoftJob.Core.Models;

namespace CloudsoftJob.Core.Data;

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
            Password = "Password123!"
        };

        Employers.TryAdd(NormalizeEmail(demoEmployer.User.Email), demoEmployer);
    }

    public ConcurrentDictionary<string, JobPosting> JobPostings { get; } = new();

    public ConcurrentDictionary<string, EmployerAccount> Employers { get; } = new();

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }
}
