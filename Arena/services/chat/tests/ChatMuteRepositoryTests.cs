using Microsoft.Extensions.Configuration;
using ChatService.Entities;
using ChatService.Repositories;
using Xunit;

namespace ChatService.Tests;

public class ChatMuteRepositoryTests
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

        var exception = Record.Exception(() => new ChatMuteRepository(configuration));
        Assert.Null(exception);
    }

    [Fact]
    public async Task InsertAsync_WithMissingConnectionString_ThrowsInvalidOperation()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var repository = new ChatMuteRepository(configuration);

        var mute = new ChatMute
        {
            UserId = 1,
            MutedBy = 2,
            Reason = "Testing",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.InsertAsync(mute));
    }

    [Fact]
    public async Task IsUserMutedAsync_WithMissingConnectionString_ThrowsInvalidOperation()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var repository = new ChatMuteRepository(configuration);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.IsUserMutedAsync(1));
    }

    [Fact]
    public async Task GetActiveMuteAsync_WithMissingConnectionString_ThrowsInvalidOperation()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var repository = new ChatMuteRepository(configuration);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.GetActiveMuteAsync(1));
    }
}
