using System.Collections.Concurrent;
using CloudsoftJob.Core.Models;

namespace CloudsoftJob.Core.Data;

public interface IInMemoryDatabase
{
    ConcurrentDictionary<string, JobPosting> JobPostings { get; }

    ConcurrentDictionary<string, EmployerAccount> Employers { get; }
}
