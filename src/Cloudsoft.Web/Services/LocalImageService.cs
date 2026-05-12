using Cloudsoft.Core.Storage;
using Microsoft.AspNetCore.Hosting;

namespace Cloudsoft.Web.Services;

public class LocalImageService : IImageService
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LocalImageService(
        IWebHostEnvironment webHostEnvironment,
        IHttpContextAccessor httpContextAccessor)
    {
        _webHostEnvironment = webHostEnvironment;
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetImageUrl(string imageName)
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        var baseUrl = $"{request?.Scheme}://{request?.Host}";
        var imagePath = imageName.TrimStart('/');

        return $"{baseUrl}/{imagePath}";
    }
}
