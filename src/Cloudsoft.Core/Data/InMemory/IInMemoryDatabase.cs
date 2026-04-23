using System.Collections.Concurrent;
using Cloudsoft.Core.Models;

namespace Cloudsoft.Core.Data;

public interface IInMemoryDatabase
{
    ConcurrentDictionary<string, JobPosting> JobPostings { get; }

    ConcurrentDictionary<string, EmployerAccount> Employers { get; }
}
