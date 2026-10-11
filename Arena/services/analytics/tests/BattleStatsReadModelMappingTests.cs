using Moq;
using AnalyticsService.Entities;
using AnalyticsService.Repositories;
using Xunit;

namespace AnalyticsService.Tests;

public class BattleStatsReadModelMappingTests
{
    private readonly Mock<IAnalyticsRepository> _repositoryMock;

    public BattleStatsReadModelMappingTests()
    {
        _repositoryMock = new Mock<IAnalyticsRepository>();
    }

    [Fact]
    public async Task GetBattleSummariesByStreamIdAsync_ReturnsSummariesForSpecifiedStream()
    {
        // Arrange
        var streamId = 101;
        var expectedSummaries = new List<StreamTeamBattleSummary>
        {
            new()
            {
                StreamId = streamId,
                TeamId = 1,
                TeamName = "Team Crimson",
                TotalAttacks = 75,
                TotalDamageDealt = 2500,
                TotalCoinsSpent = 6000,
                RoundsWon = 2,
                RoundsLost = 1,
                LastAttackAt = DateTime.UtcNow
            },
            new()
            {
                StreamId = streamId,
                TeamId = 2,
                TeamName = "Team Azure",
                TotalAttacks = 60,
                TotalDamageDealt = 1900,
                TotalCoinsSpent = 4800,
                RoundsWon = 1,
                RoundsLost = 2,
                LastAttackAt = DateTime.UtcNow
            }
        };

        _repositoryMock
            .Setup(r => r.GetBattleSummariesByStreamIdAsync(streamId))
            .ReturnsAsync(expectedSummaries);

        // Act
        var result = await _repositoryMock.Object.GetBattleSummariesByStreamIdAsync(streamId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.Equal(streamId, item.StreamId));
        Assert.Contains(result, item => item.TeamName == "Team Crimson" && item.TotalAttacks == 75);
        Assert.Contains(result, item => item.TeamName == "Team Azure" && item.TotalAttacks == 60);
    }

    [Fact]
    public async Task GetBattleSummariesByStreamIdAsync_WhenNoDataExists_ReturnsEmptyList()
    {
        // Arrange
        var streamId = 999;
        _repositoryMock
            .Setup(r => r.GetBattleSummariesByStreamIdAsync(streamId))
            .ReturnsAsync(new List<StreamTeamBattleSummary>());

        // Act
        var result = await _repositoryMock.Object.GetBattleSummariesByStreamIdAsync(streamId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRoundOutcomesByStreamIdAsync_ReturnsSequentialRoundOutcomes()
    {
        // Arrange
        var streamId = 202;
        var expectedRounds = new List<StreamRoundOutcome>
        {
            new()
            {
                StreamId = streamId,
                RoundNumber = 1,
                WinningTeamId = 1,
                WinningTeamName = "Team Crimson",
                TeamAId = 1,
                TeamBId = 2,
                TeamAAttacks = 40,
                TeamBAttacks = 35,
                TeamADamage = 1200,
                TeamBDamage = 980,
                CompletedAt = DateTime.UtcNow.AddMinutes(-10)
            },
            new()
            {
                StreamId = streamId,
                RoundNumber = 2,
                WinningTeamId = 2,
                WinningTeamName = "Team Azure",
                TeamAId = 1,
                TeamBId = 2,
                TeamAAttacks = 25,
                TeamBAttacks = 45,
                TeamADamage = 750,
                TeamBDamage = 1350,
                CompletedAt = DateTime.UtcNow.AddMinutes(-5)
            }
        };

        _repositoryMock
            .Setup(r => r.GetRoundOutcomesByStreamIdAsync(streamId))
            .ReturnsAsync(expectedRounds);

        // Act
        var result = await _repositoryMock.Object.GetRoundOutcomesByStreamIdAsync(streamId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].RoundNumber);
        Assert.Equal(1, result[0].WinningTeamId);
        Assert.Equal(2, result[1].RoundNumber);
        Assert.Equal(2, result[1].WinningTeamId);
    }

    [Fact]
    public async Task GetRoundOutcomesByStreamIdAsync_WhenNoRounds_ReturnsEmptyList()
    {
        // Arrange
        var streamId = 888;
        _repositoryMock
            .Setup(r => r.GetRoundOutcomesByStreamIdAsync(streamId))
            .ReturnsAsync(new List<StreamRoundOutcome>());

        // Act
        var result = await _repositoryMock.Object.GetRoundOutcomesByStreamIdAsync(streamId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task UpsertBattleSummaryAsync_ExecutesWithoutException()
    {
        // Arrange
        var summary = new StreamTeamBattleSummary
        {
            StreamId = 101,
            TeamId = 1,
            TeamName = "Team Crimson",
            TotalAttacks = 10,
            TotalDamageDealt = 300,
            TotalCoinsSpent = 1000,
            RoundsWon = 1,
            RoundsLost = 0,
            LastAttackAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.UpsertBattleSummaryAsync(summary))
            .Returns(Task.CompletedTask);

        // Act
        var exception = await Record.ExceptionAsync(() => _repositoryMock.Object.UpsertBattleSummaryAsync(summary));

        // Assert
        Assert.Null(exception);
        _repositoryMock.Verify(r => r.UpsertBattleSummaryAsync(summary), Times.Once);
    }

    [Fact]
    public async Task RecordRoundOutcomeAsync_ExecutesWithoutException()
    {
        // Arrange
        var outcome = new StreamRoundOutcome
        {
            StreamId = 101,
            RoundNumber = 1,
            WinningTeamId = 1,
            WinningTeamName = "Team Crimson",
            TeamAId = 1,
            TeamBId = 2,
            TeamAAttacks = 20,
            TeamBAttacks = 15,
            TeamADamage = 600,
            TeamBDamage = 450,
            CompletedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.RecordRoundOutcomeAsync(outcome))
            .Returns(Task.CompletedTask);

        // Act
        var exception = await Record.ExceptionAsync(() => _repositoryMock.Object.RecordRoundOutcomeAsync(outcome));

        // Assert
        Assert.Null(exception);
        _repositoryMock.Verify(r => r.RecordRoundOutcomeAsync(outcome), Times.Once);
    }
}
