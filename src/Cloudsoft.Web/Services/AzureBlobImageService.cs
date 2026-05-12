using Cloudsoft.Core.Options;
using Cloudsoft.Core.Storage;
using Microsoft.Extensions.Options;

namespace Cloudsoft.Web.Services;

public class AzureBlobImageService : IImageService
{
    private readonly string _blobContainerUrl;
    public AzureBlobImageService(IOptions<AzureBlobOptions> options)
    {
        _blobContainerUrl = options.Value.ContainerUrl;
    }
    public string GetImageUrl(string imageName)
    {
        var imagePath = imageName.TrimStart('/');

        if (string.IsNullOrWhiteSpace(_blobContainerUrl))
        {
            return $"/{imagePath}";
        }

        var containerUrl = _blobContainerUrl.TrimEnd('/');
        if (imagePath.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
        {
            imagePath = imagePath["images/".Length..];
        }

        return $"{containerUrl}/{imagePath}";
    }
}
