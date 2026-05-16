using Cloudsoft.Core.Models;
using Cloudsoft.Core.Services.Interfaces;

namespace Cloudsoft.Core.Services;

public class CountryLookupService : ICountryLookupService
{
    private static readonly IReadOnlyList<CountryItem> Countries =
    [
        new("SE", "Sweden"),
        new("NO", "Norway"),
        new("DK", "Denmark"),
        new("FI", "Finland"),
        new("DE", "Germany"),
        new("GB", "United Kingdom"),
        new("US", "United States"),
        new("CA", "Canada"),
        new("IN", "India"),
        new("NG", "Nigeria"),
        new("ZA", "South Africa"),
        new("OTHER", "Other")
    ];

    public Task<IReadOnlyList<CountryItem>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Countries);
    }

    public bool IsKnownCountryCode(string countryCode)
    {
        return Countries.Any(country => country.Code.Equals(countryCode, StringComparison.OrdinalIgnoreCase));
    }
}
