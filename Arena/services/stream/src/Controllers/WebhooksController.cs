using Microsoft.AspNetCore.Mvc;     // bring ASP.NET core mvc classes
using System.Security.Cryptography; // for payload processing
using System.Text;
using System.Text.Json;

namespace YourNameSpace.Controller;

[ApiController]             // web api controller
[Route("api/[controller]")] // map endpoints url to api/webhooks

public class WebhooksController : ControllerBase
{
    private readonly IConfiguration _configuration;     // private fields to dependency injection
    private readonly ITwitchEventSubValidator _validator;
    private readonly IWebhookLogRepository _webhookLogRepository;
    private readonly ILogger<WebhooksController> _logger;

    /* Constructor with Dependency Injection */
    public WebhooksController(
        IConfiguration configuration,
        ITwitchEventSubValidator validator,
        IWebhookLogRepository webhookLogRepository,
        ILogger<WebhooksController> logger)
    {
        _configuration = configuration;
        _validator = validator;
        _webhookLogRepository = webhookLogRepository;
        _logger = logger;
    }

    /* read row request body and security header */
    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        if (!Request.Headers.TryGetValue("X-Signature",out var signatureHeader))    // safely check whether signature exists
        {
            _logger.LogWarning("Webhook request missing signature header.");    // if missing,log warning for diagnostics
            return Unauthorized("Missing signature header.");       // return 401 unauthorized message
        }
        /*read raw request body */
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);   // read request stream as text
        var rawPayload = await reader.ReadToEndAsync();// read entire body stream into string payload
        /* Validate Payload */
        if(string.IsNullOrEmpty(rawPayload))    // check if payload is empty
        {
            return BadRequest("Empty payload.");    // if empty, return 400 bad request
        }
    }

    [HttpPost("twitch")]
    [AllowAnonymous]
    public async Task<IActionResult> ReceiveTwitchWebhook()
    {
        Request.EnableBuffering();  // Enable stream buffering to read raw bytes
        /* Extract required Twitch EventSub security headers */
        if (!Request.Headers.TryGetValue("Twitch-Eventsub-Message-Id", out var messageIdHeader)||
            !Request.Headers.TryGetValue("Twitch-Eventsub-Message-Timestamp", out var timestampHeader)||
            !Request.Headers.TryGetValue("Twitch-Eventsub-Message-Timestamp", out var signatureHeader)||
            !Request.Headers.TryGetValue("Twitch-Eventsub-Message-Timestamp", out var messageTypeHeader))
            {
                _logger.LogWarning("Twitch EventSub webhook rejected: Missing required security headers.");
                return StatusCodes(StatusCodes.Status403Forbidden, "Missing required Twitch headers.");
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
        
    }
}