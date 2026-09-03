/*
    strongly-typed data transfer objects (DTOs) for incoming Twitch EventSub webhooks.
    maps JSON snake_case attributes sent by Twitch servers to standard C# PascalCase properties.
*/

using System.Text.Json;
using System.Text.Json.Serialization;

namespace StreamService.DTOs;

/*
    universal outer envelope received for all Twitch EventSub webhook delivery types:
    - webhook_callback_verification (initial subscription challenge handshake)
    - notification (live stream events e.g., stream.online, stream.offline)
    - revocation (subscription cancelled/authorization revoked by broadcaster)
*/
public class TwitchEventSubEnvelope
{
    /*
        random verification challenge string dispatched exclusively during webhook creation.
        the server must echo this exact string back with a 200 OK text/plain response.
        nullable because standard notification payloads do not contain this field.
    */
    [JsonPropertyName("challenge")]
    public string? Challenge { get; set; }

    /*
        immutable subscription header containing rule definitions, subscription IDs, and event triggers
    */
    [JsonPropertyName("subscription")]
    public TwitchSubscriptionMetadata Subscription { get; set; } = new();

    /*
        polymorphic raw event payload node.
        retained as JsonElement so the controller can dynamically deserialize into specific
        event models (e.g., TwitchStreamOnlineEvent) based on Subscription.Type.
    */
    [JsonPropertyName("event")]
    public JsonElement? Event { get; set; }
}

/*
    subscription metadata detailing subscription status and criteria
*/
public class TwitchSubscriptionMetadata
{
    // unique UUID identifying the registered EventSub subscription on Twitch Helix
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    // lifecycle status of the subscription (e.g., "enabled", "webhook_callback_verification_pending")
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    // event type trigger name (e.g., "stream.online", "stream.offline")
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    // subscription schema version (e.g., "1")
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    // specific channel/broadcaster filters configured for this webhook subscription
    [JsonPropertyName("condition")]
    public TwitchCondition Condition { get; set; } = new();

    // UTC timestamp recording when the subscription was initially created
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

/*
    condition criteria linking subscription events to a specific Twitch broadcaster
*/
public class TwitchCondition
{
    // numerical Twitch broadcaster user ID to filter notifications for (e.g., "12826")
    [JsonPropertyName("broadcaster_user_id")]
    public string? BroadcasterUserId { get; set; }
}

/*
    deserialized event payload dispatched when a monitored channel goes live ('stream.online')
*/
public class TwitchStreamOnlineEvent
{
    // unique Twitch stream broadcast identifier
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    // Twitch user ID of the channel broadcasting live
    [JsonPropertyName("broadcaster_user_id")]
    public string BroadcasterUserId { get; set; } = string.Empty;

    // lowercase URL login handle of the broadcaster (e.g., "twitchdev")
    [JsonPropertyName("broadcaster_user_login")]
    public string BroadcasterUserLogin { get; set; } = string.Empty;

    // display name of the broadcaster with preserved capitalization
    [JsonPropertyName("broadcaster_user_name")]
    public string BroadcasterUserName { get; set; } = string.Empty;

    // stream broadcast category type (e.g., "live", "watch_party")
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    // RFC 3339 UTC timestamp indicating the exact moment the broadcast started
    [JsonPropertyName("started_at")]
    public DateTime StartedAt { get; set; }
}

/*
    deserialized event payload dispatched when a broadcast terminates ('stream.offline')
*/
public class TwitchStreamOfflineEvent
{
    // Twitch user ID of the channel that ended its broadcast
    [JsonPropertyName("broadcaster_user_id")]
    public string BroadcasterUserId { get; set; } = string.Empty;

    // lowercase URL login handle of the offline broadcaster
    [JsonPropertyName("broadcaster_user_login")]
    public string BroadcasterUserLogin { get; set; } = string.Empty;

    // display name of the broadcaster with preserved capitalization
    [JsonPropertyName("broadcaster_user_name")]
    public string BroadcasterUserName { get; set; } = string.Empty;
}