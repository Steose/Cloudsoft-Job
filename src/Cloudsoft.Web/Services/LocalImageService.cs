using Cloudsoft.Core.Storage;
using Microsoft.AspNetCore.Http;

namespace Cloudsoft.Web.Services;

public class LocalImageService : IImageService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LocalImageService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetImageUrl(string imageName)
    {
        // Build the relative URL to the image in the wwwroot/images folder
        var request = _httpContextAccessor.HttpContext?.Request;
        var baseUrl = $"{request?.Scheme}://{request?.Host}";

        return $"{baseUrl}/images/{imageName}";
    }
}
