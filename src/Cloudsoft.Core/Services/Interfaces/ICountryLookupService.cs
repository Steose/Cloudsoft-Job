using Cloudsoft.Core.Models;

namespace Cloudsoft.Core.Services.Interfaces;

public interface ICountryLookupService
{
    Task<IReadOnlyList<CountryItem>> GetCountriesAsync(CancellationToken cancellationToken = default);

    bool IsKnownCountryCode(string countryCode);
}
