/*
    unit tests verifying JSON snake_case mappings and polymorphic deserialization
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
    public void Deserialize_StreamOnlinePayload_MapsAllProperties()
    {
        const string json = @"{
            ""subscription"": {
                ""id"": ""sub_live_01"",
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

        var envelope = JsonSerializer.Deserialize<TwitchEventSubEnvelope>(json, _jsonOptions);

        Assert.NotNull(envelope);
        Assert.Equal("stream.online", envelope.Subscription.Type);
        Assert.Equal("1337", envelope.Subscription.Condition.BroadcasterUserId);

        Assert.True(envelope.Event.HasValue);
        var onlineEvent = envelope.Event.Value.Deserialize<TwitchStreamOnlineEvent>(_jsonOptions);

        Assert.NotNull(onlineEvent);
        Assert.Equal("live_stream_999", onlineEvent.Id);
        Assert.Equal("1337", onlineEvent.BroadcasterUserId);
        Assert.Equal("esl_csgo", onlineEvent.BroadcasterUserLogin);
        Assert.Equal("ESL_CSGO", onlineEvent.BroadcasterUserName);
        Assert.Equal("live", onlineEvent.Type);
        Assert.Equal(DateTime.Parse("2026-09-02T10:00:00Z").ToUniversalTime(), onlineEvent.StartedAt.ToUniversalTime());
    }

    [Fact]
    public void Deserialize_StreamOfflinePayload_MapsAllProperties()
    {
        const string json = @"{
            ""subscription"": {
                ""id"": ""sub_offline_01"",
                ""type"": ""stream.offline""
            },
            ""event"": {
                ""broadcaster_user_id"": ""1337"",
                ""broadcaster_user_login"": ""esl_csgo"",
                ""broadcaster_user_name"": ""ESL_CSGO""
            }
        }";

        var envelope = JsonSerializer.Deserialize<TwitchEventSubEnvelope>(json, _jsonOptions);

        Assert.NotNull(envelope);
        Assert.True(envelope.Event.HasValue);

        var offlineEvent = envelope.Event.Value.Deserialize<TwitchStreamOfflineEvent>(_jsonOptions);
        Assert.NotNull(offlineEvent);
        Assert.Equal("1337", offlineEvent.BroadcasterUserId);
        Assert.Equal("esl_csgo", offlineEvent.BroadcasterUserLogin);
        Assert.Equal("ESL_CSGO", offlineEvent.BroadcasterUserName);
    }
}