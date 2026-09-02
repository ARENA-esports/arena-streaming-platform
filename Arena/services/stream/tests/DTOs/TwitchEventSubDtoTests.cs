/*
    unit tests for Twitch EventSub DTOs verifying polymorphic event 
    deserialization and snake_case attribute mappings
*/

using System.Text.Json;
using StreamService.DTOs;
using Xunit;

namespace StreamService.Tests.DTOs;

public class TwitchEventSubDtoTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void Deserialize_StreamOnlinePayload_MapsPropertiesCorrectly()
    {
        // Arrange
        const string json = @"{
            ""subscription"": {
                ""id"": ""f1c2a-test-sub"",
                ""status"": ""enabled"",
                ""type"": ""stream.online"",
                ""version"": ""1"",
                ""condition"": {
                    ""broadcaster_user_id"": ""1337""
                }
            },
            ""event"": {
                ""id"": ""live_stream_999"",
                ""broadcaster_user_id"": ""1337"",
                ""broadcaster_user_login"": ""esl_csgo"",
                ""broadcaster_user_name"": ""ESL_CSGO"",
                ""type"": ""live"",
                ""started_at"": ""2026-09-02T10:00:00Z""
            }
        }";

        // Act
        var envelope = JsonSerializer.Deserialize<TwitchEventSubEnvelope>(json, _jsonOptions);

        // Assert
        Assert.NotNull(envelope);
        Assert.NotNull(envelope.Subscription);
        Assert.Equal("stream.online", envelope.Subscription.Type);
        Assert.Equal("1337", envelope.Subscription.Condition?.BroadcasterUserId);

        Assert.True(envelope.Event.HasValue);
        var onlineEvent = envelope.Event.Value.Deserialize<TwitchStreamOnlineEvent>(_jsonOptions);

        Assert.NotNull(onlineEvent);
        Assert.Equal("live_stream_999", onlineEvent.Id);
        Assert.Equal("1337", onlineEvent.BroadcasterUserId);
        Assert.Equal("esl_csgo", onlineEvent.BroadcasterUserLogin);
        Assert.Equal("ESL_CSGO", onlineEvent.BroadcasterUserName);
        Assert.Equal("live", onlineEvent.Type);
        Assert.Equal(DateTimeOffset.Parse("2026-09-02T10:00:00Z"), onlineEvent.StartedAt);
    }

    [Fact]
    public void Deserialize_StreamOfflinePayload_MapsPropertiesCorrectly()
    {
        // Arrange
        const string json = @"{
            ""subscription"": {
                ""id"": ""f2b3c-test-sub"",
                ""status"": ""enabled"",
                ""type"": ""stream.offline"",
                ""version"": ""1"",
                ""condition"": {
                    ""broadcaster_user_id"": ""1337""
                }
            },
            ""event"": {
                ""broadcaster_user_id"": ""1337"",
                ""broadcaster_user_login"": ""esl_csgo"",
                ""broadcaster_user_name"": ""ESL_CSGO""
            }
        }";

        // Act
        var envelope = JsonSerializer.Deserialize<TwitchEventSubEnvelope>(json, _jsonOptions);

        // Assert
        Assert.NotNull(envelope);
        Assert.NotNull(envelope.Subscription);
        Assert.Equal("stream.offline", envelope.Subscription.Type);

        Assert.True(envelope.Event.HasValue);
        var offlineEvent = envelope.Event.Value.Deserialize<TwitchStreamOfflineEvent>(_jsonOptions);

        Assert.NotNull(offlineEvent);
        Assert.Equal("1337", offlineEvent.BroadcasterUserId);
        Assert.Equal("esl_csgo", offlineEvent.BroadcasterUserLogin);
        Assert.Equal("ESL_CSGO", offlineEvent.BroadcasterUserName);
    }

    [Fact]
    public void Deserialize_CallbackVerificationPayload_MapsChallengeString()
    {
        // Arrange
        const string json = @"{
            ""challenge"": ""p9gK23lP09mZ11qRsTuVwXyZ"",
            ""subscription"": {
                ""id"": ""f3c4d-test-sub"",
                ""status"": ""webhook_callback_verification_pending"",
                ""type"": ""stream.online"",
                ""version"": ""1""
            }
        }";

        // Act
        var envelope = JsonSerializer.Deserialize<TwitchEventSubEnvelope>(json, _jsonOptions);

        // Assert
        Assert.NotNull(envelope);
        Assert.Equal("p9gK23lP09mZ11qRsTuVwXyZ", envelope.Challenge);
        Assert.Equal("f3c4d-test-sub", envelope.Subscription?.Id);
    }
}