using EventContracts;

namespace TournamentService.Services;

public interface IKafkaProducerService
{
    Task PublishTeamChangedAsync(TeamChangedEvent evt);
}
