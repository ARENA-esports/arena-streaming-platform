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
    private readonly IStreamStatusService _streamStatusService;
    private readonly ILogger<WebhooksController> _logger;

    /* Constructor with Dependency Injection */
    public WebhooksController(
        ITwitchEventSubValidator validator,
        IWebhookLogRepository webhookLogRepository,
        IStreamRepository streamRepository,
        IStreamStatusService streamStatusService,
        ILogger<WebhooksController> logger)
    {
        _validator = validator;
        _webhookLogRepository = webhookLogRepository;
        _streamRepository = streamRepository;
        _streamStatusService = streamStatusService;
        _logger = logger;
    }

    [HttpPost("twitch")]
    [AllowAnonymous]
    [RequestSizeLimit(1048576)] // Enforce 1 MB maximum payload ceiling
    public async Task<IActionResult> ReceiveTwitchWebhook(
        [FromHeader(Name = "Twitch-Eventsub-Message-Id")] string? messageId = null, //bind Twitch headers as method parameters so Swagger UI renders input fields
        [FromHeader(Name = "Twitch-Eventsub-Message-Timestamp")] string? timestamp = null,
        [FromHeader(Name = "Twitch-Eventsub-Message-Signature")] string? signature = null,
        [FromHeader(Name = "Twitch-Eventsub-Message-Type")] string? messageType = null)
    {
        Request.EnableBuffering();  // Enable stream buffering to read raw bytes

        // /* Extract required Twitch EventSub security headers */
        // if (!Request.Headers.TryGetValue("Twitch-Eventsub-Message-Id", out var messageIdHeader) ||
        //     !Request.Headers.TryGetValue("Twitch-Eventsub-Message-Timestamp", out var timestampHeader) ||
        //     !Request.Headers.TryGetValue("Twitch-Eventsub-Message-Signature", out var signatureHeader) ||
        //     !Request.Headers.TryGetValue("Twitch-Eventsub-Message-Type", out var messageTypeHeader))
        // {
        //     _logger.LogWarning("Twitch EventSub webhook rejected: Missing required security headers.");
        //     return StatusCode(StatusCodes.Status403Forbidden, "Missing required Twitch headers.");
        // }
        // Fallback to Request.Headers when model binding does not run (during direct unit test calls)
        messageId ??= Request.Headers["Twitch-Eventsub-Message-Id"].FirstOrDefault();
        timestamp ??= Request.Headers["Twitch-Eventsub-Message-Timestamp"].FirstOrDefault();
        signature ??= Request.Headers["Twitch-Eventsub-Message-Signature"].FirstOrDefault();
        messageType ??= Request.Headers["Twitch-Eventsub-Message-Type"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(messageId)||
            string.IsNullOrWhiteSpace(timestamp)||
            string.IsNullOrWhiteSpace(signature)||
            string.IsNullOrWhiteSpace(messageType))
        {
            _logger.LogWarning("Twitch EventSub webhook rejected: Missing required security headers.");
            return StatusCode(StatusCodes.Status403Forbidden, "Missing required Twitch headers.");
        }

        // /* Convert Headers to Strings */
        // string messageId = messageIdHeader.ToString();
        // string timestamp = timestampHeader.ToString();
        // string signature = signatureHeader.ToString();
        // string messageType = messageTypeHeader.ToString();

        // Sanitize user input against Log Forging / Log Injection by stripping CR and LF characters
        var safeMessageId = messageId.Replace("\r", string.Empty).Replace("\n", string.Empty);
        var safeTimestamp = timestamp.Replace("\r", string.Empty).Replace("\n", string.Empty);
        var safeSignature = signature.Replace("\r", string.Empty).Replace("\n", string.Empty);
        var safeMessageType = messageType.Replace("\r", string.Empty).Replace("\n", string.Empty);

        /* Validate Signature Header Format (Malformed -> 400 Bad Request) */
        if (!safeSignature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Twitch EventSub webhook rejected: Malformed signature header {Signature}.", safeSignature);
            return BadRequest("Malformed Twitch signature header.");
        }

        // Sanitize user input against Log Forging / Log Injection by stripping CR and LF characters
        var safeMessageId = messageId.Replace("\r", string.Empty).Replace("\n", string.Empty);
        var safeTimestamp = timestamp.Replace("\r", string.Empty).Replace("\n", string.Empty);
        var safeSignature = signature.Replace("\r", string.Empty).Replace("\n", string.Empty);

        // Restore Tier 1 check — malformed signature returns 400 Bad Request
        if (!signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Twitch EventSub webhook rejected: Malformed signature header {Signature}.", safeSignature);
            return BadRequest("Malformed Twitch signature header.");
        }
        
        /* Validate Timestamp Against Replay Attacks */
        if (!_validator.IsTimestampValid(safeTimestamp))
        {
            _logger.LogWarning("Twitch EventSub webhook rejected: Expired or invalid timestamp {Timestamp}.", safeTimestamp);
            return StatusCode(StatusCodes.Status403Forbidden, "Invalid or expired timestamp.");
        }

        /* Read raw body bytes and rewind stream position */
        using var memoryStream = new MemoryStream();    // allocate memory buffer to copy incoming body
        await Request.Body.CopyToAsync(memoryStream);   // copy entire stream asynchronously
        var rawBody = memoryStream.ToArray();           // extract raw byte array

        // /* reset stream pointer for downstream JSON */
        // Request.Body.Position = 0;

        /* Verify HMAC-SHA256 signature */
        if (!_validator.VerifySignature(messageId, timestamp, rawBody, signature))
        {
            _logger.LogWarning("Twitch EventSub webhook rejected: HMAC signature verification failed for Message ID {MessageId}.", safeMessageId);
            return StatusCode(StatusCodes.Status403Forbidden, "Invalid HMAC-SHA256 signature.");
        }

        /* Deserialize verified JSON */
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true }; // ensures snake_case or casing variances
        var envelope = JsonSerializer.Deserialize<TwitchEventSubEnvelope>(rawBody, jsonOptions); // read the rewinded request stream to memory

        if (envelope == null)
        {
            _logger.LogWarning("Twitch EventSub webhook payload could not be deserialized.");
            return BadRequest("Invalid JSON payload.");
        }

        /* Handle Challenge Handshake */
        if (messageType == "webhook_callback_verification") // check message type
        {
            // Sanitize subscription ID before logging
            var safeSubId = envelope.Subscription.Id.Replace("\r",string.Empty).Replace("\n",string.Empty);
            _logger.LogInformation("Twitch challenge received for subscription {SubId}.", safeSubId);    // log informational message that challenge received(for diagnostics)
            return Content(envelope.Challenge ?? string.Empty, "text/plain");   // ensure response has correct content type and add fallback
        }

        /* Handle Notifications */
        if (messageType == "notification")
        {
            try
            {
                // Strip 'sha256=' prefix upfront so only the 64-char hex digest is passed into MySQL
                var rawHash = signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)
                    ? signature[7..]
                    : signature;
                
                // Atomic insert-first deduplication pattern replacing the previous separate MessageExistsAsync check
                // attempts immediate insertion so MySQL primary key constraint acts as the source of truth, preventing race conditions
                var isNewDelivery = await _webhookLogRepository.TryLogMessageAsync(
                    messageId: messageId,
                    streamId: null,
                    messageType: messageType,
                    subscriptionType: envelope.Subscription.Type,
                    payloadHash: rawHash    // Use rawHash instead of raw signature
                );

                // new: if insertion fails (MySQL 1062 ER_DUP_ENTRY), acknowledge receipt with 200 OK but halt further processing
                if (!isNewDelivery)
                {
                    // log diagnostic trace that a duplicate delivery was ignored
                    _logger.LogInformation("Duplicate webhook message {MessageId} ignored: delivery already recorded.", safeMessageId);
                    return Ok(); // return 200 OK so Twitch marks delivery successful and stops retrying
                }

                // validate the event JSON
                if (!envelope.Event.HasValue ||                                  // checks whether the nullable json is populated
                    envelope.Event.Value.ValueKind == JsonValueKind.Null ||      // ensure the payload contains an actual JSON object
                    envelope.Event.Value.ValueKind == JsonValueKind.Undefined)
                {
                    _logger.LogWarning("Twitch notification message {MessageId} contains an empty event node.", safeMessageId);
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
                        // delegate state machine transition to service and capture stream id
                        affectedStreamId = await _streamStatusService.ProcessStreamStatusUpdateAsync(
                            envelope.Subscription.Type,
                            onlineEvent.BroadcasterUserName);
                    }
                    break;

                    // Route and Deserialize offline event
                    case "stream.offline":
                        var offlineEvent = envelope.Event.Value.Deserialize<TwitchStreamOfflineEvent>(jsonOptions); // convert json into strongly typed C# object
                        if (offlineEvent != null)   // null check
                        {
                            // delegate state machine transition to service and capture stream id
                            affectedStreamId = await _streamStatusService.ProcessStreamStatusUpdateAsync(
                                envelope.Subscription.Type,
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
                    payloadHash: rawHash    // pass rawHash instead of raw signature
                );

                return Ok();
            }
            catch (Exception ex)
            {
                // Transient database deadlock/timeout: return 503 so Twitch retries delivery safely
                _logger.LogError(ex, "Transient database error processing Twitch webhook {MessageId}.", safeMessageId);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "Database unavailable. Retry later.");
            }
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
