using ChatService.Services;
using Xunit;

namespace ChatService.Tests;

public class ProfanityFilterTests
{
    private readonly ProfanityFilter _filter = new();

    [Fact]
    public void Scrub_SingleBadWord_ReplacedWithAsterisks()
    {
        var result = _filter.Scrub("you are a damn fool");
        Assert.Equal("you are a *** fool", result);
    }

    [Fact]
    public void Scrub_MultipleBadWords_AllReplaced()
    {
        var result = _filter.Scrub("what the hell is this shit");
        Assert.Equal("what the *** is this ***", result);
    }

    [Fact]
    public void Scrub_CleanMessage_Unchanged()
    {
        var result = _filter.Scrub("good game well played");
        Assert.Equal("good game well played", result);
    }

    [Fact]
    public void Scrub_PartialMatch_NotReplaced()
    {
        // "class" contains "ass" but word boundary prevents false positive
        var result = _filter.Scrub("this is a class act");
        Assert.Equal("this is a class act", result);
    }

    [Fact]
    public void Scrub_CaseInsensitive_Works()
    {
        var result = _filter.Scrub("DAMN that was HELL of a game");
        Assert.Equal("*** that was *** of a game", result);
    }

    [Fact]
    public void ContainsProfanity_WithBadWord_ReturnsTrue()
    {
        Assert.True(_filter.ContainsProfanity("this is damn bad"));
    }

    [Fact]
    public void ContainsProfanity_CleanMessage_ReturnsFalse()
    {
        Assert.False(_filter.ContainsProfanity("good game well played"));
    }

    [Fact]
    public void Scrub_EmptyString_ReturnsEmpty()
    {
        Assert.Equal("", _filter.Scrub(""));
    }

    [Fact]
    public void ContainsProfanity_NullString_ReturnsFalse()
    {
        Assert.False(_filter.ContainsProfanity(null!));
    }
}
