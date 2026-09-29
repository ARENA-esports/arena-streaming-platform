using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TournamentService.Services;

/// <summary>
/// Local filesystem implementation for storing and validating uploaded files.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    public const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2 MB limit

    public static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/pjpeg",
        "image/svg+xml"
    };

    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".svg"
    };

    private readonly string _storageRootPath;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ILogger<LocalFileStorageService> logger)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;

        var configuredPath = _configuration["Storage:UploadDirectory"];
        _storageRootPath = !string.IsNullOrWhiteSpace(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, "uploads");

        if (!Directory.Exists(_storageRootPath))
        {
            Directory.CreateDirectory(_storageRootPath);
        }
    }

    /// <inheritdoc />
    public bool ValidateTeamLogo(IFormFile? file, out string? errorMessage)
    {
        if (file == null || file.Length == 0)
        {
            errorMessage = "No file was uploaded or the uploaded file is empty.";
            return false;
        }

        if (file.Length > MaxFileSizeBytes)
        {
            errorMessage = $"File size exceeds the maximum allowed limit of 2 MB ({file.Length} bytes).";
            return false;
        }

        var contentType = file.ContentType?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(contentType) || !AllowedMimeTypes.Contains(contentType))
        {
            errorMessage = $"Invalid MIME type '{file.ContentType}'. Allowed image types are PNG (image/png), JPEG (image/jpeg), and SVG (image/svg+xml).";
            return false;
        }

        var extension = Path.GetExtension(file.FileName)?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            errorMessage = $"Invalid file extension '{extension}'. Allowed extensions are .png, .jpg, .jpeg, and .svg.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    /// <inheritdoc />
    public async Task<string> SaveFileAsync(IFormFile file, string subDirectory, CancellationToken cancellationToken = default)
    {
        var targetFolder = Path.Combine(_storageRootPath, subDirectory);
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        var destinationPath = Path.Combine(targetFolder, uniqueFileName);

        await using (var targetStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(targetStream, cancellationToken);
        }

        _logger.LogInformation("Saved uploaded logo to {FilePath}", destinationPath);

        // Generate public URI
        var configuredBaseUrl = _configuration["Storage:BaseUrl"]?.TrimEnd('/');
        if (!string.IsNullOrEmpty(configuredBaseUrl))
        {
            return $"{configuredBaseUrl}/uploads/{subDirectory}/{uniqueFileName}";
        }

        return $"/uploads/{subDirectory}/{uniqueFileName}";
    }
}
