using Cloudsoft.Core.Storage;
using Microsoft.AspNetCore.Hosting;

namespace Cloudsoft.Web.Services;

public class LocalCvStorageService : ICvStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".doc",
        ".docx"
    };

    private readonly IWebHostEnvironment _webHostEnvironment;

    public LocalCvStorageService(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<string> SaveAsync(Stream cvStream, string originalFileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new InvalidOperationException("The CV file name is required.");
        }

        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Upload a CV as a PDF, DOC, or DOCX file.");
        }

        var uploadsDirectory = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "cvs");
        Directory.CreateDirectory(uploadsDirectory);

        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var destinationPath = Path.Combine(uploadsDirectory, storedFileName);

        await using var destination = File.Create(destinationPath);
        await cvStream.CopyToAsync(destination, cancellationToken);

        return storedFileName;
    }
}
