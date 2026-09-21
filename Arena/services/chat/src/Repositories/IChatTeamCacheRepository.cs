using ChatService.Entities;

namespace ChatService.Repositories;

public interface IChatTeamCacheRepository
{
    Task<ChatTeamCache?> GetByIdAsync(int teamId);
    Task UpsertAsync(ChatTeamCache team);
}
