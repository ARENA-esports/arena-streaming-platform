using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Polly;
using BattleEconomyService.Configuration;
using BattleEconomyService.DTOs;
using BattleEconomyService.Models;
using BattleEconomyService.Services;

namespace BattleEconomyService.Tests;

public class StreamServiceClientTests
{
    private readonly Mock<ILogger<StreamServiceClient>> _loggerMock = new();

    private StreamServiceClient CreateClient(HttpMessageHandler handler, string baseUrl = "http://test-stream-service/")
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl)
        };
        return new StreamServiceClient(httpClient, _loggerMock.Object);
    }

    [Fact]
    public async Task GetStreamLivenessAsync_WhenStatusIsLive_ReturnsLiveResult()
    {
        // Arrange
        const int streamId = 42;
        var json = "{\"streamId\":42,\"status\":\"Live\"}";
        var handler = new SimpleHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var client = CreateClient(handler);

        // Act
        var result = await client.GetStreamLivenessAsync(streamId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsLive);
        Assert.True(result.IsSuccess);
        Assert.Equal(streamId, result.StreamId);
        Assert.Equal(StreamLiveStatus.Live, result.Status);
    }

    [Fact]
    public async Task GetStreamLivenessAsync_WhenStatusIsLiveCaseInsensitive_ReturnsLiveResult()
    {
        // Arrange
        const int streamId = 42;
        var json = "{\"streamId\":42,\"status\":\"live\"}";
        var handler = new SimpleHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var client = CreateClient(handler);

        // Act
        var result = await client.GetStreamLivenessAsync(streamId);

        // Assert
        Assert.True(result.IsLive);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetStreamLivenessAsync_WhenStatusIsEnded_ReturnsNonLiveResult()
    {
        // Arrange
        const int streamId = 42;
        var json = "{\"streamId\":42,\"status\":\"Ended\"}";
        var handler = new SimpleHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var client = CreateClient(handler);

        // Act
        var result = await client.GetStreamLivenessAsync(streamId);

        // Assert
        Assert.False(result.IsLive);
        Assert.True(result.IsSuccess);
        Assert.Equal(StreamLiveStatus.Ended, result.Status);
    }

    [Fact]
    public async Task GetStreamLivenessAsync_WhenStatusIsScheduled_ReturnsNonLiveResult()
    {
        // Arrange
        const int streamId = 42;
        var json = "{\"streamId\":42,\"status\":\"Scheduled\"}";
        var handler = new SimpleHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var client = CreateClient(handler);

        // Act
        var result = await client.GetStreamLivenessAsync(streamId);

        // Assert
        Assert.False(result.IsLive);
        Assert.True(result.IsSuccess);
        Assert.Equal(StreamLiveStatus.Scheduled, result.Status);
    }

    [Fact]
    public async Task GetStreamLivenessAsync_WhenStatusIsCancelled_ReturnsNonLiveResult()
    {
        // Arrange
        const int streamId = 42;
        var json = "{\"streamId\":42,\"status\":\"Cancelled\"}";
        var handler = new SimpleHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var client = CreateClient(handler);

        // Act
        var result = await client.GetStreamLivenessAsync(streamId);

        // Assert
        Assert.False(result.IsLive);
        Assert.True(result.IsSuccess);
        Assert.Equal(StreamLiveStatus.Cancelled, result.Status);
    }

    [Fact]
    public async Task GetStreamLivenessAsync_When404NotFound_ReturnsNonLiveResult()
    {
        // Arrange
        const int streamId = 999;
        var handler = new SimpleHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(handler);

        // Act
        var result = await client.GetStreamLivenessAsync(streamId);

        // Assert
        Assert.False(result.IsLive);
        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.Status);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetStreamLivenessAsync_When500InternalError_FailsSafely()
    {
        // Arrange
        const int streamId = 42;
        var handler = new SimpleHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = CreateClient(handler);

        // Act
        var result = await client.GetStreamLivenessAsync(streamId);

        // Assert
        Assert.False(result.IsLive);
        Assert.False(result.IsSuccess);
        Assert.Equal("Unavailable", result.Status);
        Assert.Contains("500", result.ErrorMessage);
    }

    [Fact]
    public async Task GetStreamLivenessAsync_WhenMalformedJson_FailsSafely()
    {
        // Arrange
        const int streamId = 42;
        var handler = new SimpleHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("INVALID_JSON", Encoding.UTF8, "application/json")
        });
        var client = CreateClient(handler);

        // Act
        var result = await client.GetStreamLivenessAsync(streamId);

        // Assert
        Assert.False(result.IsLive);
        Assert.False(result.IsSuccess);
        Assert.Equal("Unavailable", result.Status);
    }

    [Fact]
    public async Task GetStreamLivenessAsync_WhenEmptyStatus_FailsSafely()
    {
        // Arrange
        const int streamId = 42;
        var json = "{\"streamId\":42,\"status\":\"\"}";
        var handler = new SimpleHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var client = CreateClient(handler);

        // Act
        var result = await client.GetStreamLivenessAsync(streamId);

        // Assert
        Assert.False(result.IsLive);
        Assert.False(result.IsSuccess);
        Assert.Equal("Unavailable", result.Status);
    }

    [Fact]
    public async Task GetStreamLivenessAsync_WhenNetworkException_FailsSafely()
    {
        // Arrange
        const int streamId = 42;
        var handler = new ThrowingHttpMessageHandler(new HttpRequestException("Connection refused"));
        var client = CreateClient(handler);

        // Act
        var result = await client.GetStreamLivenessAsync(streamId);

        // Assert
        Assert.False(result.IsLive);
        Assert.False(result.IsSuccess);
        Assert.Equal("Unavailable", result.Status);
        Assert.Contains("network error", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetStreamLivenessAsync_WhenTimeoutOccurs_FailsSafelyWithoutThrowing()
    {
        // Arrange
        const int streamId = 42;
        // In HttpClient, timeout throws OperationCanceledException with a token not cancelled by caller
        var handler = new ThrowingHttpMessageHandler(new TaskCanceledException("HttpClient timeout"));
        var client = CreateClient(handler);

        // Act
        var result = await client.GetStreamLivenessAsync(streamId, CancellationToken.None);

        // Assert
        Assert.False(result.IsLive);
        Assert.False(result.IsSuccess);
        Assert.Equal("Unavailable", result.Status);
        Assert.Contains("timed out", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResiliencePolicy_WhenTransient500Occurs_RetriesAndSucceeds()
    {
        // Arrange: ServiceCollection setup matching Program.cs DI
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<StreamServiceOptions>(opts =>
        {
            opts.BaseUrl = "http://test-stream-service/";
            opts.TimeoutSeconds = 2;
            opts.RetryCount = 1;
        });

        var callCount = 0;
        var testHandler = new SimpleHttpMessageHandler(_ =>
        {
            callCount++;
            if (callCount == 1)
            {
                // First call fails with transient 503
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
            // Second call succeeds
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"streamId\":101,\"status\":\"Live\"}", Encoding.UTF8, "application/json")
            };
        });

        services.AddHttpClient<IStreamServiceClient, StreamServiceClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<StreamServiceOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        })
        .AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(
            retryCount: 1,
            sleepDurationProvider: _ => TimeSpan.FromMilliseconds(1)
        ))
        .ConfigurePrimaryHttpMessageHandler(() => testHandler);

        var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<IStreamServiceClient>();

        // Act
        var result = await client.GetStreamLivenessAsync(101);

        // Assert
        Assert.Equal(2, callCount); // Retried once on transient failure
        Assert.True(result.IsLive);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ResiliencePolicy_When404NotFoundOccurs_DoesNotRetry()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<StreamServiceOptions>(opts =>
        {
            opts.BaseUrl = "http://test-stream-service/";
            opts.TimeoutSeconds = 2;
            opts.RetryCount = 1;
        });

        var callCount = 0;
        var testHandler = new SimpleHttpMessageHandler(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        services.AddHttpClient<IStreamServiceClient, StreamServiceClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<StreamServiceOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        })
        .AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(
            retryCount: 1,
            sleepDurationProvider: _ => TimeSpan.FromMilliseconds(1)
        ))
        .ConfigurePrimaryHttpMessageHandler(() => testHandler);

        var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<IStreamServiceClient>();

        // Act
        var result = await client.GetStreamLivenessAsync(101);

        // Assert
        Assert.Equal(1, callCount); // 404 must NOT be retried
        Assert.False(result.IsLive);
        Assert.Equal("NotFound", result.Status);
    }

    // Helper fake handlers
    private class SimpleHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public SimpleHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }

    private class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Exception _exception;

        public ThrowingHttpMessageHandler(Exception exception)
        {
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromException<HttpResponseMessage>(_exception);
        }
    }
}
