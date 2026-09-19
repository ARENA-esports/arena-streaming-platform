using Moq;
using Microsoft.Extensions.Configuration;
using ChatService.Entities;
using ChatService.Repositories;
using Xunit;

namespace ChatService.Tests;

public class ChatMessageRepositoryTests
{
    /// <summary>
    /// Verifies that the repository can be constructed with a valid configuration.
    /// The actual database insert is covered by integration tests against a live MySQL container.
    /// </summary>
    [Fact]
    public void Constructor_WithValidConfiguration_DoesNotThrow()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "ConnectionStrings:ChatDb", "Server=localhost;Port=3306;Database=arena_chat_db;Uid=root;Pwd=test;" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act & Assert — construction should not throw
        var exception = Record.Exception(() => new ChatMessageRepository(configuration));
        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies that accessing the connection string throws when not configured.
    /// This validates the fail-fast behavior on misconfiguration.
    /// </summary>
    [Fact]
    public async Task InsertAsync_WithMissingConnectionString_ThrowsInvalidOperation()
    {
        // Arrange — no connection string configured
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var repository = new ChatMessageRepository(configuration);

        var message = new ChatMessage
        {
            TeamId = 1,
            UserId = 42,
            Username = "TestUser",
            Content = "Hello"
        };

        // Act & Assert — should throw InvalidOperationException for missing connection string
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.InsertAsync(message));
    }

    /// <summary>
    /// Verifies the ChatMessage entity has correct default values and property mapping.
    /// </summary>
    [Fact]
    public void ChatMessage_Entity_HasCorrectDefaults()
    {
        // Arrange & Act
        var message = new ChatMessage();

        // Assert
        Assert.Equal(0, message.MessageId);
        Assert.Equal(0, message.TeamId);
        Assert.Equal(0, message.UserId);
        Assert.Equal(string.Empty, message.Username);
        Assert.Equal(string.Empty, message.Content);
    }

    /// <summary>
    /// Verifies the ChatMessage entity correctly holds assigned values.
    /// </summary>
    [Fact]
    public void ChatMessage_Entity_HoldsAssignedValues()
    {
        // Arrange & Act
        var now = DateTime.UtcNow;
        var message = new ChatMessage
        {
            MessageId = 1,
            TeamId = 5,
            UserId = 42,
            Username = "TestUser",
            Content = "Hello faction!",
            CreatedAt = now
        };

        // Assert
        Assert.Equal(1, message.MessageId);
        Assert.Equal(5, message.TeamId);
        Assert.Equal(42, message.UserId);
        Assert.Equal("TestUser", message.Username);
        Assert.Equal("Hello faction!", message.Content);
        Assert.Equal(now, message.CreatedAt);
    }
}
