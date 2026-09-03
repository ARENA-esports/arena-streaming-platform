using Microsoft.AspNetCore.Mvc;     // bring ASP.NET core mvc classes
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography; // for payload processing
using StreamService.DTOs;
using StreamService.Repositories;
using StreamService.Services;
using System.Text;
using System.Text.Json;

namespace StreamService.Controllers;

[ApiController]             // web api controller
[Route("api/[controller]")] // map endpoints url to api/webhooks
public class WebhooksController : ControllerBase
{
    // private fields to dependency injection
    private readonly ITwitchEventSubValidator _validator;
    private readonly IWebhookLogRepository _webhookLogRepository;
    private readonly IStreamRepository _streamRepository;
    private readonly ILogger<WebhooksController> _logger;

    /* Constructor with Dependency Injection */
    public WebhooksController(
        ITwitchEventSubValidator validator,
        IWebhookLogRepository webhookLogRepository,
        IStreamRepository streamRepository,
        ILogger<WebhooksController> logger)
    {
        _validator = validator;
        _webhookLogRepository = webhookLogRepository;
        _streamRepository = streamRepository;
        _logger = logger;
    }

    [HttpPost("twitch")]
    [AllowAnonymous]
    public async Task<IActionResult> ReceiveTwitchWebhook()
    {
        Request.EnableBuffering();  // Enable stream buffering to read raw bytes

        /* Extract required Twitch EventSub security headers */
        if (!Request.Headers.TryGetValue("Twitch-Eventsub-Message-Id", out var messageIdHeader) ||
            !Request.Headers.TryGetValue("Twitch-Eventsub-Message-Timestamp", out var timestampHeader) ||
            !Request.Headers.TryGetValue("Twitch-Eventsub-Message-Signature", out var signatureHeader) ||
            !Request.Headers.TryGetValue("Twitch-Eventsub-Message-Type", out var messageTypeHeader))
        {
            _logger.LogWarning("Twitch EventSub webhook rejected: Missing required security headers.");
            return StatusCode(StatusCodes.Status403Forbidden, "Missing required Twitch headers.");
        }

        /* Convert Headers to Strings */
        string messageId = messageIdHeader.ToString();
        string timestamp = timestampHeader.ToString();
        string signature = signatureHeader.ToString();
        string messageType = messageTypeHeader.ToString();

        /* Validate Timestamp Against Replay Attacks */
        if (!_validator.IsTimestampValid(timestamp))
        {
            _logger.LogWarning("Twitch EventSub webhook rejected: Expired or invalid timestamp {Timestamp}.", timestamp);
            return StatusCode(StatusCodes.Status403Forbidden, "Invalid or expired timestamp.");
        }

        /* Read raw body bytes and rewind stream position */
        using var memoryStream = new MemoryStream();    // allocate memory buffer to copy incoming body
        await Request.Body.CopyToAsync(memoryStream);   // copy entire stream asynchronously
        var rawBody = memoryStream.ToArray();           // extract raw byte array

        /* reset stream pointer for downstream JSON */
        Request.Body.Position = 0;

        /* Verify HMAC-SHA256 signature */
        if (!_validator.VerifySignature(messageId, timestamp, rawBody, signature))
        {
            _logger.LogWarning("Twitch EventSub webhook rejected: HMAC signature verification failed for Message ID {MessageId}.", messageId);
            return StatusCode(StatusCodes.Status403Forbidden, "Invalid HMAC-SHA256 signature.");
        }

        /* Deserialize verified JSON */
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true }; // ensures snake_case or casing variances
        var envelope = await JsonSerializer.DeserializeAsync<TwitchEventSubEnvelope>(Request.Body, jsonOptions); // read the rewinded request stream to memory

        if (envelope == null)
        {
            _logger.LogWarning("Twitch EventSub webhook payload could not be deserialized.");
            return BadRequest("Invalid JSON payload.");
        }

        /* Handle Challenge Handshake */
        if (messageType == "webhook_callback_verification") // check message type
        {
            _logger.LogInformation("Twitch challenge received for subscription {SubId}.", envelope.Subscription.Id);    // log informational message that challenge received(for diagnostics)
            return Content(envelope.Challenge ?? string.Empty, "text/plain");   // ensure response has correct content type and add fallback
        }

        /* Handle Notifications */
        if (messageType == "notification")
        {
            /* Deduplicate incoming notifications */
            if (await _webhookLogRepository.MessageExistsAsync(messageId))  // check if message_id has recorded
            {
                _logger.LogInformation("Duplicate webhook message {MessageId} ignored.", messageId);
                return Ok();
            }

            // validate the event JSON
            if (!envelope.Event.HasValue ||                                  // checks whether the nullable json is populated
                envelope.Event.Value.ValueKind == JsonValueKind.Null ||      // ensure the payload contains an actual JSON object
                envelope.Event.Value.ValueKind == JsonValueKind.Undefined)
            {
                _logger.LogWarning("Twitch notification message {MessageId} contains an empty event node.", messageId);
                return Ok();
            }

            int? affectedStreamId = null;

            /* Route event to corresponding handler */
            switch (envelope.Subscription.Type)
            {
                // Route and Deserialize online event
                case "stream.online":
                    var onlineEvent = envelope.Event.Value.Deserialize<TwitchStreamOnlineEvent>(jsonOptions); // convert json into strongly typed C# object
                    if (onlineEvent != null)    // null check
                    {
                        // Log Live Transition
                        _logger.LogInformation(
                            "Channel {BroadcasterName} (ID: {BroadcasterId}) went live at {StartedAt}. Stream ID: {StreamId}.",
                            onlineEvent.BroadcasterUserName,    // channel name
                            onlineEvent.BroadcasterUserId,      // Twitch user ID
                            onlineEvent.StartedAt,              // exact UTC timestamp when the stream began
                            onlineEvent.Id);                    // stream id

                        // update database record to Live status and capture stream primary key
                        affectedStreamId = await _streamRepository.UpdateStreamLiveStatusAsync(
                            onlineEvent.BroadcasterUserName,
                            onlineEvent.StartedAt);
                    }
                    break;

                // Route and Deserialize offline event
                case "stream.offline":
                    var offlineEvent = envelope.Event.Value.Deserialize<TwitchStreamOfflineEvent>(jsonOptions);
                    if (offlineEvent != null)
                    {
                        // Log Offline Transition
                        _logger.LogInformation(
                            "Channel {BroadcasterName} (ID: {BroadcasterId}) went offline.",
                            offlineEvent.BroadcasterUserName,
                            offlineEvent.BroadcasterUserId);

                        // update database record to Ended status
                        affectedStreamId = await _streamRepository.UpdateStreamOfflineStatusAsync(
                            offlineEvent.BroadcasterUserName);
                    }
                    break;

                // unhandled event fallback
                default:
                    _logger.LogInformation(
                        "Unhandled Twitch EventSub subscription type received: {SubscriptionType}",
                        envelope.Subscription.Type);
                    break;
            }

            /* insert the delivery audit record and claim the message ID */
            await _webhookLogRepository.LogMessageAsync(
                messageId: messageId,
                streamId: affectedStreamId,
                messageType: messageType,
                subscriptionType: envelope.Subscription.Type,
                payloadHash: signature
            );

            return Ok();
        }

        /* Handle Revocations */
        if (messageType == "revocation")
        {
            _logger.LogWarning("Twitch subscription {SubId} revoked: {Status}", envelope.Subscription.Id, envelope.Subscription.Status);
            return Ok();
        }

        return Ok();
    }
}