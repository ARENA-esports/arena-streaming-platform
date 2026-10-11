using AnalyticsService.DTOs;
using AnalyticsService.Entities;
using Xunit;

namespace AnalyticsService.Tests;

public class StreamTeamBattleSummaryTests
{
    [Fact]
    public void StreamTeamBattleSummary_DefaultValues_AreZeroAndNull()
    {
        // Act
        var summary = new StreamTeamBattleSummary();

        // Assert
        Assert.Equal(0, summary.StreamId);
        Assert.Equal(0, summary.TeamId);
        Assert.Equal(string.Empty, summary.TeamName);
        Assert.Equal(0, summary.TotalAttacks);
        Assert.Equal(0L, summary.TotalDamageDealt);
        Assert.Equal(0L, summary.TotalCoinsSpent);
        Assert.Equal(0, summary.RoundsWon);
        Assert.Equal(0, summary.RoundsLost);
        Assert.Null(summary.LastAttackAt);
    }

    [Fact]
    public void StreamTeamBattleSummary_CanSetAndGetProperties()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var summary = new StreamTeamBattleSummary
        {
            StreamId = 42,
            TeamId = 1,
            TeamName = "Team Crimson",
            TotalAttacks = 150,
            TotalDamageDealt = 4500L,
            TotalCoinsSpent = 12000L,
            RoundsWon = 3,
            RoundsLost = 1,
            LastAttackAt = now,
            CreatedAt = now.AddHours(-1),
            UpdatedAt = now
        };

        // Assert
        Assert.Equal(42, summary.StreamId);
        Assert.Equal(1, summary.TeamId);
        Assert.Equal("Team Crimson", summary.TeamName);
        Assert.Equal(150, summary.TotalAttacks);
        Assert.Equal(4500L, summary.TotalDamageDealt);
        Assert.Equal(12000L, summary.TotalCoinsSpent);
        Assert.Equal(3, summary.RoundsWon);
        Assert.Equal(1, summary.RoundsLost);
        Assert.Equal(now, summary.LastAttackAt);
        Assert.Equal(now.AddHours(-1), summary.CreatedAt);
        Assert.Equal(now, summary.UpdatedAt);
    }

    [Fact]
    public void StreamTeamBattleResponse_MapsFromEntity_Correctly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var entity = new StreamTeamBattleSummary
        {
            StreamId = 10,
            TeamId = 2,
            TeamName = "Team Azure",
            TotalAttacks = 85,
            TotalDamageDealt = 2300L,
            TotalCoinsSpent = 7500L,
            RoundsWon = 2,
            RoundsLost = 2,
            LastAttackAt = now
        };

        // Act
        var response = new StreamTeamBattleResponse
        {
            StreamId = entity.StreamId,
            TeamId = entity.TeamId,
            TeamName = entity.TeamName,
            TotalAttacks = entity.TotalAttacks,
            TotalDamageDealt = entity.TotalDamageDealt,
            TotalCoinsSpent = entity.TotalCoinsSpent,
            RoundsWon = entity.RoundsWon,
            RoundsLost = entity.RoundsLost,
            LastAttackAt = entity.LastAttackAt
        };

        // Assert
        Assert.Equal(10, response.StreamId);
        Assert.Equal(2, response.TeamId);
        Assert.Equal("Team Azure", response.TeamName);
        Assert.Equal(85, response.TotalAttacks);
        Assert.Equal(2300L, response.TotalDamageDealt);
        Assert.Equal(7500L, response.TotalCoinsSpent);
        Assert.Equal(2, response.RoundsWon);
        Assert.Equal(2, response.RoundsLost);
        Assert.Equal(now, response.LastAttackAt);
    }
}
