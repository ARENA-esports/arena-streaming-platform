/*
    unit tests for WebhooksController verifying Twitch EventSub verification handshake,
    timestamp replay guards, signature checks, deduplication, and lifecycle transitions
*/

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using StreamService.Controllers;
using StreamService.Repositories;
using StreamService.Services;
using Xunit;

namespace StreamService.Tests.Controllers;

public class WebhooksControllerTests
{
    private readonly Mock<ITwitchEventSubValidator> _validatorMock;
    private readonly Mock<IWebhookLogRepository> _webhookLogRepoMock;
    private readonly Mock<IStreamRepository> _streamRepoMock;
    private readonly Mock<ILogger<WebhooksController>> _loggerMock;
    private readonly WebhooksController _controller;

    public WebhooksControllerTests()
    {
        _validatorMock = new Mock<ITwitchEventSubValidator>();
        _webhookLogRepoMock = new Mock<IWebhookLogRepository>();
        _streamRepoMock = new Mock<IStreamRepository>();
        _loggerMock = new Mock<ILogger<WebhooksController>>();

        _controller = new WebhooksController(
            _validatorMock.Object,
            _webhookLogRepoMock.Object,
            _streamRepoMock.Object,
            _loggerMock.Object
        );
    }

    private void SetupRequestContext(
        string? messageType = "notification",
        string? messageId = "msg_001",
        string? timestamp = null,
        string? signature = "sha256=mocked_signature",
        string jsonPayload = "{}")
    {
        var context = new DefaultHttpContext();

        if (messageType != null) context.Request.Headers["Twitch-Eventsub-Message-Type"] = messageType;
        if (messageId != null) context.Request.Headers["Twitch-Eventsub-Message-Id"] = messageId;
        if (timestamp != null) context.Request.Headers["Twitch-Eventsub-Message-Timestamp"] = timestamp;
        else context.Request.Headers["Twitch-Eventsub-Message-Timestamp"] = DateTimeOffset.UtcNow.ToString("o");
        if (signature != null) context.Request.Headers["Twitch-Eventsub-Message-Signature"] = signature;

        context.Request.ContentType = "application/json";

        var bytes = Encoding.UTF8.GetBytes(jsonPayload);
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };
    }

    /* ---------------- Header & Cryptographic Verification Tests ---------------- */

    [Fact]
    public async Task ReceiveTwitchWebhook_WhenRequiredHeadersMissing_Returns403Forbidden()
    {
        // Arrange - omitting message signature header
        SetupRequestContext(signature: null);

        // Act
        var result = await _controller.ReceiveTwitchWebhook();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        Assert.Equal("Missing required Twitch headers.", objectResult.Value);
    }

    [Fact]
    public async Task ReceiveTwitchWebhook_WhenTimestampInvalidOrExpired_Returns403Forbidden()
    {
        // Arrange
        SetupRequestContext();
        _validatorMock.Setup(v => v.IsTimestampValid(It.IsAny<string>(), 10)).Returns(false);

        // Act
        var result = await _controller.ReceiveTwitchWebhook();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        Assert.Equal("Invalid or expired timestamp.", objectResult.Value);
    }

    [Fact]
    public async Task ReceiveTwitchWebhook_WhenHmacSignatureInvalid_Returns403Forbidden()
    {
        // Arrange
        SetupRequestContext();
        _validatorMock.Setup(v => v.IsTimestampValid(It.IsAny<string>(), 10)).Returns(true);
        _validatorMock.Setup(v => v.VerifySignature(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<string>())).Returns(false);

        // Act
        var result = await _controller.ReceiveTwitchWebhook();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        Assert.Equal("Invalid HMAC-SHA256 signature.", objectResult.Value);
    }

    /* ---------------- Handshake Verification ---------------- */

    [Fact]
    public async Task ReceiveTwitchWebhook_CallbackVerificationHandshake_EchoesRawChallenge()
    {
        // Arrange
        const string expectedChallenge = "p9gK23lP09mZ11qRsTuVwXyZ";
        var payload = JsonSerializer.Serialize(new
        {
            challenge = expectedChallenge,
            subscription = new { id = "sub_handshake_1", type = "stream.online" }
        });

        SetupRequestContext(messageType: "webhook_callback_verification", jsonPayload: payload);
        _validatorMock.Setup(v => v.IsTimestampValid(It.IsAny<string>(), 10)).Returns(true);
        _validatorMock.Setup(v => v.VerifySignature(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<string>())).Returns(true);

        // Act
        var result = await _controller.ReceiveTwitchWebhook();

        // Assert
        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal("text/plain", contentResult.ContentType);
        Assert.Equal(expectedChallenge, contentResult.Content);
    }

    /* ---------------- Notification Deduplication & Lifecycle ---------------- */

    [Fact]
    public async Task ReceiveTwitchWebhook_DuplicateMessage_Returns200OkWithoutReprocessing()
    {
        // Arrange
        const string messageId = "msg_duplicate_101";
        var payload = "{\"subscription\":{\"id\":\"sub_1\",\"type\":\"stream.online\"}}";

        SetupRequestContext(messageType: "notification", messageId: messageId, jsonPayload: payload);
        _validatorMock.Setup(v => v.IsTimestampValid(It.IsAny<string>(), 10)).Returns(true);
        _validatorMock.Setup(v => v.VerifySignature(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<string>())).Returns(true);
        _webhookLogRepoMock.Setup(w => w.MessageExistsAsync(messageId)).ReturnsAsync(true);

        // Act
        var result = await _controller.ReceiveTwitchWebhook();

        // Assert
        Assert.IsType<OkResult>(result);
        _streamRepoMock.Verify(s => s.UpdateStreamLiveStatusAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
        _webhookLogRepoMock.Verify(w => w.LogMessageAsync(
            It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ReceiveTwitchWebhook_StreamOnlineEvent_UpdatesStatusToLiveAndLogsAudit()
    {
        // Arrange
        const string messageId = "msg_stream_online_201";
        const string broadcasterLogin = "esl_csgo";
        var startedAt = DateTimeOffset.UtcNow;

        var envelopeJson = $@"{{
            ""subscription"": {{
                ""id"": ""sub_live_1"",
                ""type"": ""stream.online"",
                ""version"": ""1""
            }},
            ""event"": {{
                ""id"": ""stream_999"",
                ""broadcaster_user_id"": ""123456"",
                ""broadcaster_user_name"": ""{broadcasterLogin}"",
                ""broadcaster_user_login"": ""{broadcasterLogin}"",
                ""type"": ""live"",
                ""started_at"": ""{startedAt:O}""
            }}
        }}";

        SetupRequestContext(messageType: "notification", messageId: messageId, jsonPayload: envelopeJson);
        _validatorMock.Setup(v => v.IsTimestampValid(It.IsAny<string>(), 10)).Returns(true);
        _validatorMock.Setup(v => v.VerifySignature(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<string>())).Returns(true);
        _webhookLogRepoMock.Setup(w => w.MessageExistsAsync(messageId)).ReturnsAsync(false);
        _streamRepoMock.Setup(s => s.UpdateStreamLiveStatusAsync(broadcasterLogin, It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(50); // returns affected stream ID

        // Act
        var result = await _controller.ReceiveTwitchWebhook();

        // Assert
        Assert.IsType<OkResult>(result);
        _streamRepoMock.Verify(s => s.UpdateStreamLiveStatusAsync(broadcasterLogin, It.IsAny<DateTimeOffset>()), Times.Once);
        _webhookLogRepoMock.Verify(w => w.LogMessageAsync(
            messageId, 50, "notification", "stream.online", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ReceiveTwitchWebhook_StreamOfflineEvent_UpdatesStatusToEndedAndLogsAudit()
    {
        // Arrange
        const string messageId = "msg_stream_offline_301";
        const string broadcasterLogin = "esl_csgo";

        var envelopeJson = $@"{{
            ""subscription"": {{
                ""id"": ""sub_off_1"",
                ""type"": ""stream.offline"",
                ""version"": ""1""
            }},
            ""event"": {{
                ""broadcaster_user_id"": ""123456"",
                ""broadcaster_user_name"": ""{broadcasterLogin}"",
                ""broadcaster_user_login"": ""{broadcasterLogin}""
            }}
        }}";

        SetupRequestContext(messageType: "notification", messageId: messageId, jsonPayload: envelopeJson);
        _validatorMock.Setup(v => v.IsTimestampValid(It.IsAny<string>(), 10)).Returns(true);
        _validatorMock.Setup(v => v.VerifySignature(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<string>())).Returns(true);
        _webhookLogRepoMock.Setup(w => w.MessageExistsAsync(messageId)).ReturnsAsync(false);
        _streamRepoMock.Setup(s => s.UpdateStreamOfflineStatusAsync(broadcasterLogin))
            .ReturnsAsync(50);

        // Act
        var result = await _controller.ReceiveTwitchWebhook();

        // Assert
        Assert.IsType<OkResult>(result);
        _streamRepoMock.Verify(s => s.UpdateStreamOfflineStatusAsync(broadcasterLogin), Times.Once);
        _webhookLogRepoMock.Verify(w => w.LogMessageAsync(
            messageId, 50, "notification", "stream.offline", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ReceiveTwitchWebhook_RevocationEvent_LogsAndReturns200Ok()
    {
        // Arrange
        const string messageId = "msg_revocation_401";
        var payload = "{\"subscription\":{\"id\":\"sub_revoked\",\"type\":\"stream.online\",\"status\":\"user_removed\"}}";

        SetupRequestContext(messageType: "revocation", messageId: messageId, jsonPayload: payload);
        _validatorMock.Setup(v => v.IsTimestampValid(It.IsAny<string>(), 10)).Returns(true);
        _validatorMock.Setup(v => v.VerifySignature(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<string>())).Returns(true);

        // Act
        var result = await _controller.ReceiveTwitchWebhook();

        // Assert
        Assert.IsType<OkResult>(result);
        _streamRepoMock.Verify(s => s.UpdateStreamLiveStatusAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
        _streamRepoMock.Verify(s => s.UpdateStreamOfflineStatusAsync(It.IsAny<string>()), Times.Never);
    }
}