namespace StreamService.Services;

public interface ITwitchEventSubValidator
{
    bool IsTimestampValid(string? timestampHeader, int maxAgeMinutes = 10);     // validates that the webhook request
    bool VerifySignature(string? messageId, string? timestamp, byte[] rawBody,string? signatureHeader); // verifies the raw payload
}