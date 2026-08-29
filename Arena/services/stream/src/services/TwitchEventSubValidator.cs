using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace StreamService.Services;

public class TwitchEventSubValidator: ITwitchEventSubValidator
{
    /* Constructor and Inject Webhook Secret */
    private readonly string _webhookSecret;     // dependency injection
    public TwitchEventSubValidator(IConfiguration configuration)
    {
        _webhookSecret = configuration["TwitchSettings:EventSubSecret"]  // extract sign in secret
        ?? throw new InvalidOperationException("Twitch:WebhookSecret is not configured.");  // ensure fail fast startup behavior
    }
    
    /* Timestamp Verification */
    public bool IsTimestampValid(string? timestampHeader, int maxAgeMinutes = 10)
    {
        if (string.IsNullOrWhiteSpace(timestampHeader)) // check if header is missing,empty or just whitespace
        {
            return false;
        }
        /*  parse timestamp string into datetime object.
            rfc 3339 -> datetime
        */
        if (!DateTimeOffset.TryParse(timestampHeader, out var messageTime))
        {
            return false;   // if fail to parse
        }
        var age = DateTimeOffset.UtcNow - messageTime;  // calculate how old request is
        /*
        ensure timestamp is'n future and not older than buffer
        */
        return age >= TimeSpan.Zero && age <=TimeSpan.FromMinutes(maxAgeMinutes);
    }
    /* HMAC-SHA256 Signature Verification */
    public bool VerifySignature(string? messageId,string? timestamp, byte[] rawBody, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(messageId)||  // prevent null reference errors
            string.IsNullOrWhiteSpace(timestamp)||
            string.IsNullOrWhiteSpace(signatureHeader)||
            rawBody == null ||
            rawBody.Length == 0)
            {
                return false;
            }
        
        /* Allocate combined buffer and copy prefix + raw body */
        var prefixBytes = Encoding.UTF8.GetBytes(messageId+timestamp); //convert header prefix to bytes
        var combinedData = new byte[prefixBytes.Length + rawBody.Length];// allocate single contiguous buffer for total payload
        Buffer.BlockCopy(prefixBytes,0,combinedData,0,prefixBytes.Length);//fast copy bytes to buffer
        Buffer.BlockCopy(rawBody,0,combinedData,prefixBytes.Length,rawBody.Length);

        /* Compute HMAC-SHA256 hash */
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_webhookSecret));// creates a disposable hasher
        var hashBytes = hmac.ComputeHash(combinedData);//run hmac over payload and produce cryptographic hash

        /* format expected signature string */
        var expectedSignature = "sha256="+ Convert.ToHexStringLower(hashBytes);// convert 32 hash to 64 char lower

        /* convert signatures to bytes for fixed-time evaluation */
        var expectedBytes = Encoding.UTF8.GetBytes(expectedSignature);
        var actualBytes = Encoding.UTF8.GetBytes(signatureHeader);
        if (expectedBytes.Length != actualBytes.Length)// guard against length mismatches
        {
            return false;
        }
        return CryptographicOperations.FixedTimeEquals(expectedBytes,actualBytes);
    }

}