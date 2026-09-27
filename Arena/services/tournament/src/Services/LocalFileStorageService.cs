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
        // guard againts missing zero bytes uploads
        if (file == null || file.Length == 0)
        {
            errorMessage = "No file was uploaded or the uploaded file is empty.";
            return false;
        }

        // enforce strict 2 MB ceiling to protect server storage and memory
        if (file.Length > MaxFileSizeBytes)
        {
            errorMessage = $"File size exceeds the maximum allowed limit of 2 MB ({file.Length} bytes).";
            return false;
        }

        // check the browser reported mime type (first line of defense)
        var contentType = file.ContentType?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(contentType) || !AllowedMimeTypes.Contains(contentType))
        {
            errorMessage = $"Invalid MIME type '{file.ContentType}'. Allowed image types are PNG, JPEG, and SVG.";
            return false;
        }

        // check the file extension (png, jpg, jpeg, svg)
        var extension = Path.GetExtension(file.FileName)?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            errorMessage = $"Invalid file extension '{extension}'. Allowed extensions are .png, .jpg, .jpeg, and .svg.";
            return false;
        }

        /*
            deep security check
            file extensions and mime types can spoofed.
            for defend from it rread first 8 raw binary bytes directly from the file stream to confirm its real identity.
        */
        // Binary Magic-Byte Inspection to prevent header spoofing
        using var stream = file.OpenReadStream();
        var header = new byte[8];
        int bytesRead = stream.Read(header, 0, header.Length);

        if (bytesRead < 4)
        {
            errorMessage = "Uploaded file header is corrupted or incomplete.";
            return false;
        }

        // PNG standard signature: always starts with bytes 89 50 4E 47
        bool isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;

        // JPEG standard signature: always starts with bytes FF D8 FF
        bool isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;

        // SVG check: SVG is XML text, so we check if the beginning contains XML or SVG opening tags
        stream.Position = 0;
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        var sampleText = reader.ReadToEnd().Substring(0, Math.Min((int)file.Length, 100)).ToLowerInvariant();
        bool isSvg = sampleText.Contains("<svg") || sampleText.Contains("<?xml");

        // Reject the file if its real binary header doesn't match any allowed format
        if (!isPng && !isJpeg && !isSvg)
        {
            errorMessage = "Security Validation Failed: File header magic bytes do not match valid PNG, JPEG, or SVG signatures.";
            return false;
        }

        // All validation layers passed
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
