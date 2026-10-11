using AnalyticsService.Entities;

namespace AnalyticsService.Tests;

public class StreamEngagementSummaryTests
{
    [Fact]
    public void StreamEngagementSummary_DefaultValues_AreZeroAndNull()
    {
        // Act
        var summary = new StreamEngagementSummary();

        // Assert
        Assert.Equal(0, summary.StreamId);
        Assert.Equal(0L, summary.TotalWatchSeconds);
        Assert.Equal(0L, summary.TotalCoinsEarned);
        Assert.Equal(0, summary.TotalWatchTicks);
        Assert.Equal(0, summary.UniqueViewers);
        Assert.Null(summary.LastEventAt);
    }

    [Fact]
    public void StreamEngagementSummary_CanSetAndGetProperties()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var summary = new StreamEngagementSummary
        {
            StreamId = 101,
            TotalWatchSeconds = 3600,
            TotalCoinsEarned = 600,
            TotalWatchTicks = 60,
            UniqueViewers = 5,
            LastEventAt = now,
            CreatedAt = now.AddHours(-1),
            UpdatedAt = now
        };

        // Assert
        Assert.Equal(101, summary.StreamId);
        Assert.Equal(3600, summary.TotalWatchSeconds);
        Assert.Equal(600, summary.TotalCoinsEarned);
        Assert.Equal(60, summary.TotalWatchTicks);
        Assert.Equal(5, summary.UniqueViewers);
        Assert.Equal(now, summary.LastEventAt);
        Assert.Equal(now.AddHours(-1), summary.CreatedAt);
        Assert.Equal(now, summary.UpdatedAt);
    }
}
