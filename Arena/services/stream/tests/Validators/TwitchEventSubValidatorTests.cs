/*
    unit tests for TwitchEventSubValidator verifying timestamp validation,
    HMAC-SHA256 signature verification, and configuration guards
*/

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Moq;
using StreamService.Services;
using Xunit;

namespace StreamService.Tests.Validators;

public class TwitchEventSubValidatorTests
{
    private const string TestSecret = "Arena_Secret_Key_For_Jwt_Token_Signing_2026_SE3022_Production_Grade!";
    private readonly TwitchEventSubValidator _validator;

    public TwitchEventSubValidatorTests()
    {
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["TwitchSettings:EventSubSecret"]).Returns(TestSecret);

        _validator = new TwitchEventSubValidator(configMock.Object);
    }

    private static string ComputeValidSignature(string messageId, string timestamp, byte[] rawBody, string secret)
    {
        var prefixBytes = Encoding.UTF8.GetBytes(messageId + timestamp);
        var combinedData = new byte[prefixBytes.Length + rawBody.Length];
        Buffer.BlockCopy(prefixBytes, 0, combinedData, 0, prefixBytes.Length);
        Buffer.BlockCopy(rawBody, 0, combinedData, prefixBytes.Length, rawBody.Length);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hashBytes = hmac.ComputeHash(combinedData);
        return "sha256=" + Convert.ToHexStringLower(hashBytes);
    }

    /* ---------------- Constructor Guard ---------------- */

    [Fact]
    public void Constructor_WhenSecretMissing_ThrowsInvalidOperationException()
    {
        var emptyConfigMock = new Mock<IConfiguration>();
        emptyConfigMock.Setup(c => c["TwitchSettings:EventSubSecret"]).Returns((string?)null);

        Assert.Throws<InvalidOperationException>(() => new TwitchEventSubValidator(emptyConfigMock.Object));
    }

    /* ---------------- Timestamp Validation Tests ---------------- */

    [Fact]
    public void IsTimestampValid_WithCurrentTimestamp_ReturnsTrue()
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("o");
        var result = _validator.IsTimestampValid(timestamp);
        Assert.True(result);
    }

    [Fact]
    public void IsTimestampValid_WhenOlderThan10Minutes_ReturnsFalse()
    {
        var expiredTimestamp = DateTimeOffset.UtcNow.AddMinutes(-11).ToString("o");
        var result = _validator.IsTimestampValid(expiredTimestamp);
        Assert.False(result);
    }

    [Fact]
public void IsTimestampValid_WhenTimestampWithinFutureDriftTolerance_ReturnsTrue()
{
    // Arrange: 2 minutes into the future (within the 5-minute drift threshold)
    var minorFutureTimestamp = DateTimeOffset.UtcNow.AddMinutes(2).ToString("o");

    // Act
    var result = _validator.IsTimestampValid(minorFutureTimestamp);

    // Assert
    Assert.True(result);
}

[Fact]
public void IsTimestampValid_WhenTimestampExceedsFutureDriftLimit_ReturnsFalse()
{
    // Arrange: 6 minutes into the future (exceeds the 5-minute drift threshold)
    var excessiveFutureTimestamp = DateTimeOffset.UtcNow.AddMinutes(6).ToString("o");

    // Act
    var result = _validator.IsTimestampValid(excessiveFutureTimestamp);

    // Assert
    Assert.False(result);
}

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-valid-timestamp")]
    public void IsTimestampValid_WhenMalformedOrEmpty_ReturnsFalse(string? timestamp)
    {
        var result = _validator.IsTimestampValid(timestamp);
        Assert.False(result);
    }

    /* ---------------- Signature Verification Tests ---------------- */

    [Fact]
    public void VerifySignature_WithValidHMACSignature_ReturnsTrue()
    {
        const string messageId = "msg_valid_001";
        var timestamp = DateTimeOffset.UtcNow.ToString("o");
        var rawBody = Encoding.UTF8.GetBytes("{\"subscription\":{\"type\":\"stream.online\"}}");
        var validSignature = ComputeValidSignature(messageId, timestamp, rawBody, TestSecret);

        var result = _validator.VerifySignature(messageId, timestamp, rawBody, validSignature);
        Assert.True(result);
    }

    [Fact]
    public void VerifySignature_WhenSignatureTampered_ReturnsFalse()
    {
        const string messageId = "msg_tampered_002";
        var timestamp = DateTimeOffset.UtcNow.ToString("o");
        var rawBody = Encoding.UTF8.GetBytes("{\"subscription\":{\"type\":\"stream.online\"}}");
        const string tamperedSignature = "sha256=0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

        var result = _validator.VerifySignature(messageId, timestamp, rawBody, tamperedSignature);
        Assert.False(result);
    }

    [Theory]
    [InlineData(null, "2026-09-02T12:00:00Z", "sha256=dummy", true)]
    [InlineData("msg_01", null, "sha256=dummy", true)]
    [InlineData("msg_01", "2026-09-02T12:00:00Z", null, true)]
    [InlineData("msg_01", "2026-09-02T12:00:00Z", "sha256=dummy", false)]
    public void VerifySignature_WhenRequiredDataMissing_ReturnsFalse(
        string? messageId, string? timestamp, string? signature, bool provideBody)
    {
        var body = provideBody ? Encoding.UTF8.GetBytes("{\"test\":true}") : Array.Empty<byte>();
        var result = _validator.VerifySignature(messageId, timestamp, body, signature);
        Assert.False(result);
    }
}