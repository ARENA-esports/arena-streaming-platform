/*
    unit tests for WebhooksController verifying Twitch EventSub headers,
    challenge handshake verification, atomic deduplication (Story 101),
    and lifecycle transitions
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
        string? signature = "sha256=valid_mocked_signature",
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
    public async Task ReceiveTwitchWebhook_WhenHeadersMissing_Returns403Forbidden()
    {
        SetupRequestContext(signature: null);

        var result = await _controller.ReceiveTwitchWebhook();

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        Assert.Equal("Missing required Twitch headers.", objectResult.Value);
    }

    [Fact]
    public async Task ReceiveTwitchWebhook_WhenTimestampInvalid_Returns403Forbidden()
    {
        SetupRequestContext();
        _validatorMock.Setup(v => v.IsTimestampValid(It.IsAny<string>(), 10)).Returns(false);

        var result = await _controller.ReceiveTwitchWebhook();

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        Assert.Equal("Invalid or expired timestamp.", objectResult.Value);
    }

    [Fact]
    public async Task ReceiveTwitchWebhook_WhenSignatureFails_Returns403Forbidden()
    {
        SetupRequestContext();
        _validatorMock.Setup(v => v.IsTimestampValid(It.IsAny<string>(), 10)).Returns(true);
        _validatorMock.Setup(v => v.VerifySignature(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>())).Returns(false);

        var result = await _controller.ReceiveTwitchWebhook();

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        Assert.Equal("Invalid HMAC-SHA256 signature.", objectResult.Value);
    }

    /* ---------------- Handshake Verification ---------------- */

    [Fact]
    public async Task ReceiveTwitchWebhook_CallbackVerificationHandshake_EchoesChallengeText()
    {
        const string expectedChallenge = "p9gK23lP09mZ11qRsTuVwXyZ";
        var payload = JsonSerializer.Serialize(new
        {
            challenge = expectedChallenge,
            subscription = new { id = "sub_handshake_1", type = "stream.online" }
        });

        SetupRequestContext(messageType: "webhook_callback_verification", jsonPayload: payload);
        _validatorMock.Setup(v => v.IsTimestampValid(It.IsAny<string>(), 10)).Returns(true);
        _validatorMock.Setup(v => v.VerifySignature(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>())).Returns(true);

        var result = await _controller.ReceiveTwitchWebhook();

        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal("text/plain", contentResult.ContentType);
        Assert.Equal(expectedChallenge, contentResult.Content);
    }

    /* ---------------- Story 101: Idempotent Deduplication & Lifecycle ---------------- */

    [Fact]
    public async Task ReceiveTwitchWebhook_DuplicateMessage_Returns200OkWithoutUpdatingDatabase()
    {
        const string messageId = "msg_duplicate_001";
        var payload = JsonSerializer.Serialize(new
        {
            subscription = new { id = "sub_1", type = "stream.online" }
        });

        SetupRequestContext(messageType: "notification", messageId: messageId, jsonPayload: payload);
        _validatorMock.Setup(v => v.IsTimestampValid(It.IsAny<string>(), 10)).Returns(true);
        _validatorMock.Setup(v => v.VerifySignature(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>())).Returns(true);

        // Story 101: TryLogMessageAsync returns false on duplicate key conflicts (MySQL Error 1062)
        _webhookLogRepoMock.Setup(w => w.TryLogMessageAsync(
            messageId,
            null,
            "notification",
            "stream.online",
            It.IsAny<string>())).ReturnsAsync(false);

        var result = await _controller.ReceiveTwitchWebhook();

        Assert.IsType<OkResult>(result);
        _streamRepoMock.Verify(s => s.UpdateStreamLiveStatusAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
        _streamRepoMock.Verify(s => s.UpdateStreamOfflineStatusAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ReceiveTwitchWebhook_StreamOnlineEvent_UpdatesStatusToLiveAndLogsAudit()
    {
        const string messageId = "msg_stream_online_101";
        const string broadcasterName = "esl_csgo";
        var startedAt = DateTimeOffset.UtcNow;

        var payload = JsonSerializer.Serialize(new
        {
            subscription = new
            {
                id = "sub_live_1",
                type = "stream.online",
                version = "1"
            },
            @event = new
            {
                id = "stream_live_555",
                broadcaster_user_id = "123456",
                broadcaster_user_name = broadcasterName,
                broadcaster_user_login = broadcasterName,
                type = "live",
                started_at = startedAt.ToString("o")
            }
        });

        SetupRequestContext(messageType: "notification", messageId: messageId, jsonPayload: payload);
        _validatorMock.Setup(v => v.IsTimestampValid(It.IsAny<string>(), 10)).Returns(true);
        _validatorMock.Setup(v => v.VerifySignature(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>())).Returns(true);

        // Story 101: Atomic insert succeeds for fresh delivery
        _webhookLogRepoMock.Setup(w => w.TryLogMessageAsync(
            messageId,
            null,
            "notification",
            "stream.online",
            It.IsAny<string>())).ReturnsAsync(true);

        _streamRepoMock.Setup(s => s.UpdateStreamLiveStatusAsync(broadcasterName, It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(10);

        var result = await _controller.ReceiveTwitchWebhook();

        Assert.IsType<OkResult>(result);
        _streamRepoMock.Verify(s => s.UpdateStreamLiveStatusAsync(broadcasterName, It.IsAny<DateTimeOffset>()), Times.Once);
        _webhookLogRepoMock.Verify(w => w.TryLogMessageAsync(
            messageId, null, "notification", "stream.online", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ReceiveTwitchWebhook_StreamOfflineEvent_UpdatesStatusToEndedAndLogsAudit()
    {
        const string messageId = "msg_stream_offline_202";
        const string broadcasterName = "esl_csgo";

        var payload = JsonSerializer.Serialize(new
        {
            subscription = new
            {
                id = "sub_offline_1",
                type = "stream.offline",
                version = "1"
            },
            @event = new
            {
                broadcaster_user_id = "123456",
                broadcaster_user_name = broadcasterName,
                broadcaster_user_login = broadcasterName
            }
        });

        SetupRequestContext(messageType: "notification", messageId: messageId, jsonPayload: payload);
        _validatorMock.Setup(v => v.IsTimestampValid(It.IsAny<string>(), 10)).Returns(true);
        _validatorMock.Setup(v => v.VerifySignature(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>())).Returns(true);

        // Story 101: Atomic insert succeeds for fresh delivery
        _webhookLogRepoMock.Setup(w => w.TryLogMessageAsync(
            messageId,
            null,
            "notification",
            "stream.offline",
            It.IsAny<string>())).ReturnsAsync(true);

        _streamRepoMock.Setup(s => s.UpdateStreamOfflineStatusAsync(broadcasterName))
            .ReturnsAsync(10);

        var result = await _controller.ReceiveTwitchWebhook();

        Assert.IsType<OkResult>(result);
        _streamRepoMock.Verify(s => s.UpdateStreamOfflineStatusAsync(broadcasterName), Times.Once);
        _webhookLogRepoMock.Verify(w => w.TryLogMessageAsync(
            messageId, null, "notification", "stream.offline", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ReceiveTwitchWebhook_RevocationEvent_Returns200OkWithoutStreamUpdates()
    {
        const string messageId = "msg_revocation_303";
        var payload = JsonSerializer.Serialize(new
        {
            subscription = new { id = "sub_revoked_1", status = "user_removed" }
        });

        SetupRequestContext(messageType: "revocation", messageId: messageId, jsonPayload: payload);
        _validatorMock.Setup(v => v.IsTimestampValid(It.IsAny<string>(), 10)).Returns(true);
        _validatorMock.Setup(v => v.VerifySignature(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>())).Returns(true);

        var result = await _controller.ReceiveTwitchWebhook();

        Assert.IsType<OkResult>(result);
        _streamRepoMock.Verify(s => s.UpdateStreamLiveStatusAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
        _streamRepoMock.Verify(s => s.UpdateStreamOfflineStatusAsync(It.IsAny<string>()), Times.Never);
    }
}