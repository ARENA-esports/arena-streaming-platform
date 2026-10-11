using AnalyticsService.DTOs;
using AnalyticsService.Entities;
using Xunit;

namespace AnalyticsService.Tests;

public class StreamRoundOutcomeTests
{
    [Fact]
    public void StreamRoundOutcome_DefaultValues_AreZeroAndEmpty()
    {
        // Act
        var outcome = new StreamRoundOutcome();

        // Assert
        Assert.Equal(0, outcome.StreamId);
        Assert.Equal(0, outcome.RoundNumber);
        Assert.Equal(0, outcome.WinningTeamId);
        Assert.Equal(string.Empty, outcome.WinningTeamName);
        Assert.Equal(0, outcome.TeamAId);
        Assert.Equal(0, outcome.TeamBId);
        Assert.Equal(0, outcome.TeamAAttacks);
        Assert.Equal(0, outcome.TeamBAttacks);
        Assert.Equal(0, outcome.TeamADamage);
        Assert.Equal(0, outcome.TeamBDamage);
    }

    [Fact]
    public void StreamRoundOutcome_CanSetAndGetProperties()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var outcome = new StreamRoundOutcome
        {
            StreamId = 55,
            RoundNumber = 1,
            WinningTeamId = 1,
            WinningTeamName = "Team Crimson",
            TeamAId = 1,
            TeamBId = 2,
            TeamAAttacks = 30,
            TeamBAttacks = 22,
            TeamADamage = 850,
            TeamBDamage = 620,
            CompletedAt = now,
            CreatedAt = now
        };

        // Assert
        Assert.Equal(55, outcome.StreamId);
        Assert.Equal(1, outcome.RoundNumber);
        Assert.Equal(1, outcome.WinningTeamId);
        Assert.Equal("Team Crimson", outcome.WinningTeamName);
        Assert.Equal(1, outcome.TeamAId);
        Assert.Equal(2, outcome.TeamBId);
        Assert.Equal(30, outcome.TeamAAttacks);
        Assert.Equal(22, outcome.TeamBAttacks);
        Assert.Equal(850, outcome.TeamADamage);
        Assert.Equal(620, outcome.TeamBDamage);
        Assert.Equal(now, outcome.CompletedAt);
        Assert.Equal(now, outcome.CreatedAt);
    }

    [Fact]
    public void StreamRoundOutcomeResponse_MapsFromEntity_Correctly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var entity = new StreamRoundOutcome
        {
            StreamId = 77,
            RoundNumber = 2,
            WinningTeamId = 2,
            WinningTeamName = "Team Azure",
            TeamAId = 1,
            TeamBId = 2,
            TeamAAttacks = 15,
            TeamBAttacks = 28,
            TeamADamage = 450,
            TeamBDamage = 910,
            CompletedAt = now
        };

        // Act
        var response = new StreamRoundOutcomeResponse
        {
            StreamId = entity.StreamId,
            RoundNumber = entity.RoundNumber,
            WinningTeamId = entity.WinningTeamId,
            WinningTeamName = entity.WinningTeamName,
            TeamAId = entity.TeamAId,
            TeamBId = entity.TeamBId,
            TeamAAttacks = entity.TeamAAttacks,
            TeamBAttacks = entity.TeamBAttacks,
            TeamADamage = entity.TeamADamage,
            TeamBDamage = entity.TeamBDamage,
            CompletedAt = entity.CompletedAt
        };

        // Assert
        Assert.Equal(77, response.StreamId);
        Assert.Equal(2, response.RoundNumber);
        Assert.Equal(2, response.WinningTeamId);
        Assert.Equal("Team Azure", response.WinningTeamName);
        Assert.Equal(1, response.TeamAId);
        Assert.Equal(2, response.TeamBId);
        Assert.Equal(15, response.TeamAAttacks);
        Assert.Equal(28, response.TeamBAttacks);
        Assert.Equal(450, response.TeamADamage);
        Assert.Equal(910, response.TeamBDamage);
        Assert.Equal(now, response.CompletedAt);
    }
}
