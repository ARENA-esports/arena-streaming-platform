using Microsoft.Extensions.Logging;
using AnalyticsService.DTOs;
using AnalyticsService.Entities;
using AnalyticsService.Repositories;

namespace AnalyticsService.Services;

/// <summary>
/// Implementation of <see cref="IBattleStatsService"/> that queries the Analytics read model via <see cref="IAnalyticsRepository"/>.
/// Maps entities to DTOs and coordinates combined dashboard data.
/// </summary>
public class BattleStatsService : IBattleStatsService
{
    private readonly IAnalyticsRepository _repository;
    private readonly ILogger<BattleStatsService> _logger;

    public BattleStatsService(
        IAnalyticsRepository repository,
        ILogger<BattleStatsService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StreamTeamBattleResponse>> GetAllBattleSummariesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching all battle summaries from repository");
        var summaries = await _repository.GetAllBattleSummariesAsync();
        return summaries.Select(MapToTeamResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StreamTeamBattleResponse>> GetBattleSummariesByStreamIdAsync(int streamId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching battle summaries for stream {StreamId} from repository", streamId);
        var summaries = await _repository.GetBattleSummariesByStreamIdAsync(streamId);
        return summaries.Select(MapToTeamResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StreamRoundOutcomeResponse>> GetRoundOutcomesByStreamIdAsync(int streamId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching round outcomes for stream {StreamId} from repository", streamId);
        var outcomes = await _repository.GetRoundOutcomesByStreamIdAsync(streamId);
        return outcomes.Select(MapToRoundResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<StreamBattleDashboardResponse> GetStreamBattleDashboardAsync(int streamId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Assembling complete battle dashboard for stream {StreamId}", streamId);
        var teamSummaries = await GetBattleSummariesByStreamIdAsync(streamId, cancellationToken);
        var roundOutcomes = await GetRoundOutcomesByStreamIdAsync(streamId, cancellationToken);

        return new StreamBattleDashboardResponse
        {
            StreamId = streamId,
            Teams = teamSummaries,
            Rounds = roundOutcomes
        };
    }

    private static StreamTeamBattleResponse MapToTeamResponse(StreamTeamBattleSummary entity) => new()
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

    private static StreamRoundOutcomeResponse MapToRoundResponse(StreamRoundOutcome entity) => new()
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
}
