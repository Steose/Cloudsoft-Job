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
        var containerUrl = _blobContainerUrl.TrimEnd('/');
        var imagePath = imageName.TrimStart('/');

        return $"{containerUrl}/{imagePath}";
    }
}
