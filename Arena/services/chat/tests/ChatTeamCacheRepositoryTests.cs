using Microsoft.Extensions.Configuration;
using ChatService.Entities;
using ChatService.Repositories;
using Xunit;

namespace ChatService.Tests;

public class ChatTeamCacheRepositoryTests
{
    [Fact]
    public void Constructor_WithValidConfiguration_DoesNotThrow()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "ConnectionStrings:ChatDb", "Server=localhost;Port=3306;Database=arena_chat_db;Uid=root;Pwd=test;" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var exception = Record.Exception(() => new ChatTeamCacheRepository(configuration));
        Assert.Null(exception);
    }

    [Fact]
    public async Task GetByIdAsync_WithMissingConnectionString_ThrowsInvalidOperation()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var repository = new ChatTeamCacheRepository(configuration);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.GetByIdAsync(1));
    }
}
