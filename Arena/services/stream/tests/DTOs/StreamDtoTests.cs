/*
    unit tests for Stream DTO validation rules and computed EmbedUrl generation
*/

using System.ComponentModel.DataAnnotations;
using StreamService.DTOs;
using Xunit;

namespace StreamService.Tests.DTOs;

public class StreamDtoTests
{
    private static IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, validationContext, validationResults, validateAllProperties: true);
        return validationResults;
    }

    /* ---------------- LinkStreamRequest Validation Tests ---------------- */

    [Theory]
    [InlineData("Twitch")]
    [InlineData("YouTube")]
    public void LinkStreamRequest_WithValidPlatform_PassesValidation(string platform)
    {
        var model = new LinkStreamRequest
        {
            ChannelName = "esl_csgo",
            Platform = platform,
            EmbedParentDomain = "localhost"
        };

        var results = ValidateModel(model);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("Kick")]
    [InlineData("Facebook")]
    [InlineData("InvalidPlatform")]
    public void LinkStreamRequest_WithInvalidPlatform_FailsValidation(string invalidPlatform)
    {
        var model = new LinkStreamRequest
        {
            ChannelName = "esl_csgo",
            Platform = invalidPlatform,
            EmbedParentDomain = "localhost"
        };

        var results = ValidateModel(model);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Platform must be either 'Twitch' or 'YouTube'"));
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("arena.gg")]
    [InlineData("staging.arena.platform.io")]
    public void LinkStreamRequest_WithValidParentDomain_PassesValidation(string validDomain)
    {
        var model = new LinkStreamRequest
        {
            ChannelName = "esl_csgo",
            Platform = "Twitch",
            EmbedParentDomain = validDomain
        };

        var results = ValidateModel(model);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("https://arena.gg")]
    [InlineData("http://localhost")]
    [InlineData("arena.gg:8080")]
    [InlineData("arena.gg/stream")]
    public void LinkStreamRequest_WithInvalidParentDomainFormat_FailsValidation(string invalidDomain)
    {
        var model = new LinkStreamRequest
        {
            ChannelName = "esl_csgo",
            Platform = "Twitch",
            EmbedParentDomain = invalidDomain
        };

        var results = ValidateModel(model);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Parent domain must be a valid hostname"));
    }

    /* ---------------- StreamResponse EmbedUrl Computed Property Tests ---------------- */

    [Fact]
    public void StreamResponse_EmbedUrl_ForTwitch_GeneratesToSCompliantIframeUrl()
    {
        var response = new StreamResponse(
            StreamId: 1,
            StreamerId: 10,
            TournamentId: 2,
            MatchId: 5,
            ChannelName: "esl_csgo",
            Platform: "Twitch",
            StreamTitle: "Grand Finals",
            EmbedParentDomain: "arena.gg",
            Status: "Live",
            ViewerCount: 5000,
            StartedAt: DateTime.UtcNow,
            EndedAt: null,
            CreatedAt: DateTime.UtcNow
        );

        Assert.Equal("https://player.twitch.tv/?channel=esl_csgo&parent=arena.gg&autoplay=false", response.EmbedUrl);
    }

    [Fact]
    public void StreamResponse_EmbedUrl_ForYouTube_GeneratesLivePlayerUrl()
    {
        var response = new StreamResponse(
            StreamId: 2,
            StreamerId: 10,
            TournamentId: 2,
            MatchId: 5,
            ChannelName: "UC_x5XG1OV2P6uZZ5FSM9Ttw",
            Platform: "YouTube",
            StreamTitle: "Quarterfinals",
            EmbedParentDomain: "localhost",
            Status: "Scheduled",
            ViewerCount: 0,
            StartedAt: null,
            EndedAt: null,
            CreatedAt: DateTime.UtcNow
        );

        Assert.Equal("https://www.youtube.com/embed/live_stream?channel=UC_x5XG1OV2P6uZZ5FSM9Ttw", response.EmbedUrl);
    }
}