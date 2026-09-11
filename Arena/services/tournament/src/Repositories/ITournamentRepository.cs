using TournamentService.DTOs;

namespace TournamentService.Repositories;

public interface ITournamentRepository
{
    Task<int> CreateTournamentAsync(string name, string seasonIdentifier, DateTimeOffset startDate, DateTimeOffset endDate);
    Task<TournamentResponse?> GetTournamentByIdAsync(int id);
    Task<IEnumerable<TournamentResponse>> GetAllTournamentsAsync();
    Task<bool> UpdateTournamentAsync(int id, string name, string seasonIdentifier, DateTimeOffset startDate, DateTimeOffset endDate);
    Task<bool> CancelTournamentAsync(int id);
}
