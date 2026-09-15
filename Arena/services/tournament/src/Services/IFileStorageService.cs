using Microsoft.AspNetCore.Http;

namespace TournamentService.Services;

/// <summary>
/// Provides file storage operations and validation for uploads.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Validates an uploaded team logo file against size (max 2 MB) and MIME types (PNG, JPEG, SVG).
    /// </summary>
    /// <param name="file">The uploaded file to validate.</param>
    /// <param name="errorMessage">Detailed error message if validation fails.</param>
    /// <returns>True if the file is valid, otherwise false.</returns>
    bool ValidateTeamLogo(IFormFile? file, out string? errorMessage);

    /// <summary>
    /// Persists an uploaded file to storage and returns its accessible public URI.
    /// </summary>
    /// <param name="file">The uploaded file.</param>
    /// <param name="subDirectory">Subdirectory to organize storage (e.g., "logos").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Publicly accessible URL of the stored file.</returns>
    Task<string> SaveFileAsync(IFormFile file, string subDirectory, CancellationToken cancellationToken = default);
}
