using EventContracts;

namespace TournamentService.Services;

public interface IKafkaProducerService
{
    Task PublishAsync<T>(string topic, string key, T message) where T : class;
    Task PublishTeamChangedAsync(TeamChangedEvent evt);
}
