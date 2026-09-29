using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TournamentService.Services;
using Xunit;

namespace TournamentService.Tests.Services;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _testTempDir;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ILogger<LocalFileStorageService>> _mockLogger;
    private readonly LocalFileStorageService _service;

    public LocalFileStorageServiceTests()
    {
        _testTempDir = Path.Combine(Path.GetTempPath(), "arena_storage_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testTempDir);

        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockEnvironment.Setup(e => e.ContentRootPath).Returns(_testTempDir);

        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["Storage:UploadDirectory"]).Returns(_testTempDir);
        _mockConfiguration.Setup(c => c["Storage:BaseUrl"]).Returns("https://assets.arena.gg");

        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockLogger = new Mock<ILogger<LocalFileStorageService>>();

        _service = new LocalFileStorageService(
            _mockEnvironment.Object,
            _mockConfiguration.Object,
            _mockHttpContextAccessor.Object,
            _mockLogger.Object
        );
    }

    public void Dispose()
    {
        if (Directory.Exists(_testTempDir))
        {
            try
            {
                Directory.Delete(_testTempDir, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    private static IFormFile CreateMockFormFile(string fileName, string contentType, long sizeBytes)
    {
        var content = new byte[sizeBytes];
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, sizeBytes, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    #region Validation Tests

    [Theory]
    [InlineData("logo.png", "image/png")]
    [InlineData("logo.jpg", "image/jpeg")]
    [InlineData("logo.jpeg", "image/jpeg")]
    [InlineData("logo.svg", "image/svg+xml")]
    public void ValidateTeamLogo_ValidImageTypesAndUnder2MB_ReturnsTrue(string fileName, string contentType)
    {
        // Arrange: 1 MB file
        var file = CreateMockFormFile(fileName, contentType, 1024 * 1024);

        // Act
        var isValid = _service.ValidateTeamLogo(file, out var errorMessage);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void ValidateTeamLogo_ExactMaxLimit2MB_ReturnsTrue()
    {
        // Arrange: Exactly 2 MB (2,097,152 bytes)
        var file = CreateMockFormFile("logo.png", "image/png", 2 * 1024 * 1024);

        // Act
        var isValid = _service.ValidateTeamLogo(file, out var errorMessage);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Theory]
    [InlineData(2 * 1024 * 1024 + 1)] // 1 byte over 2 MB
    [InlineData(3 * 1024 * 1024)]     // 3 MB
    [InlineData(10 * 1024 * 1024)]    // 10 MB
    public void ValidateTeamLogo_Exceeds2MB_ReturnsFalse(long sizeBytes)
    {
        // Arrange
        var file = CreateMockFormFile("large_logo.png", "image/png", sizeBytes);

        // Act
        var isValid = _service.ValidateTeamLogo(file, out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("exceeds the maximum allowed limit of 2 MB", errorMessage);
    }

    [Theory]
    [InlineData("document.pdf", "application/pdf")]
    [InlineData("readme.txt", "text/plain")]
    [InlineData("binary.bin", "application/octet-stream")]
    [InlineData("animation.gif", "image/gif")]
    [InlineData("graphic.webp", "image/webp")]
    [InlineData("script.js", "application/javascript")]
    public void ValidateTeamLogo_InvalidMimeType_ReturnsFalse(string fileName, string contentType)
    {
        // Arrange
        var file = CreateMockFormFile(fileName, contentType, 500 * 1024);

        // Act
        var isValid = _service.ValidateTeamLogo(file, out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("Invalid MIME type", errorMessage);
    }

    [Theory]
    [InlineData("logo.exe", "image/png")]
    [InlineData("logo.bat", "image/jpeg")]
    [InlineData("logo", "image/png")]
    public void ValidateTeamLogo_InvalidExtension_ReturnsFalse(string fileName, string contentType)
    {
        // Arrange
        var file = CreateMockFormFile(fileName, contentType, 500 * 1024);

        // Act
        var isValid = _service.ValidateTeamLogo(file, out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("Invalid file extension", errorMessage);
    }

    [Fact]
    public void ValidateTeamLogo_NullFile_ReturnsFalse()
    {
        // Act
        var isValid = _service.ValidateTeamLogo(null, out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("No file was uploaded", errorMessage);
    }

    [Fact]
    public void ValidateTeamLogo_EmptyFile_ReturnsFalse()
    {
        // Arrange: 0-byte file
        var file = CreateMockFormFile("empty.png", "image/png", 0);

        // Act
        var isValid = _service.ValidateTeamLogo(file, out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("empty", errorMessage);
    }

    #endregion

    #region SaveFile Tests

    [Fact]
    public async Task SaveFileAsync_ValidFile_SavesToDiskAndReturnsBaseUrl()
    {
        // Arrange
        const string fileContent = "dummy-svg-content";
        var bytes = Encoding.UTF8.GetBytes(fileContent);
        var stream = new MemoryStream(bytes);
        var formFile = new FormFile(stream, 0, bytes.Length, "file", "badge.svg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/svg+xml"
        };

        // Act
        var resultUrl = await _service.SaveFileAsync(formFile, "logos");

        // Assert
        Assert.StartsWith("https://assets.arena.gg/uploads/logos/", resultUrl);
        Assert.EndsWith(".svg", resultUrl);

        var savedFiles = Directory.GetFiles(Path.Combine(_testTempDir, "logos"));
        Assert.Single(savedFiles);
        Assert.Equal(fileContent, await File.ReadAllTextAsync(savedFiles[0]));
    }

    #endregion
}
