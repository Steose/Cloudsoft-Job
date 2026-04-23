using Cloudsoft.Core.Models;

namespace Cloudsoft.Core.Repositories.Interfaces;

public interface IEmployerRepository
{
    Task<EmployerAccount?> GetByEmailAsync(string email);

    Task<bool> EmailExistsAsync(string email);

    Task<EmployerAccount?> AddAsync(EmployerAccount employerAccount);
}
