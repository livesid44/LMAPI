using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LMAPI.Infrastructure;
using LMAPI.Models;
using LMAPI.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace LMAPI.Tests;

public class LogicMonitorServiceTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static HttpClient BuildHttpClient(HttpStatusCode statusCode, object? responseBody)
    {
        var json = responseBody is null ? "" : JsonSerializer.Serialize(responseBody);
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });

        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://test.logicmonitor.com/santaba/rest/")
        };
    }

    private static LogicMonitorService BuildService(HttpClient client)
        => new(client, NullLogger<LogicMonitorService>.Instance);

    // ── GetDeviceAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDeviceAsync_ReturnsDevice_WhenFound()
    {
        var device = new Device { Id = 42, DisplayName = "Server-01", Name = "192.168.1.1", AlertStatus = "normal" };
        var envelope = new LogicMonitorResponse<Device> { Status = 200, ErrorMessage = "OK", Data = device };

        using var client = BuildHttpClient(HttpStatusCode.OK, envelope);
        var result = await BuildService(client).GetDeviceAsync(42);

        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("Server-01", result.DisplayName);
    }

    [Fact]
    public async Task GetDeviceAsync_ReturnsNull_WhenNotFound()
    {
        using var client = BuildHttpClient(HttpStatusCode.NotFound, null);
        var result = await BuildService(client).GetDeviceAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetDeviceAsync_Throws_WhenApiReturnsServerError()
    {
        using var client = BuildHttpClient(HttpStatusCode.InternalServerError, null);
        await Assert.ThrowsAsync<HttpRequestException>(() => BuildService(client).GetDeviceAsync(1));
    }

    // ── GetDeviceEventsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetDeviceEventsAsync_ReturnsEvents_WhenSuccessful()
    {
        var items = new[]
        {
            new DeviceEvent { Id = "e1", DeviceId = 42, LogMessage = "CPU spike", Severity = "warn" },
            new DeviceEvent { Id = "e2", DeviceId = 42, LogMessage = "Disk full",  Severity = "error" }
        };
        var envelope = new LogicMonitorResponse<LogicMonitorListData<DeviceEvent>>
        {
            Status = 200, ErrorMessage = "OK",
            Data = new LogicMonitorListData<DeviceEvent> { Total = 2, Items = items }
        };

        using var client = BuildHttpClient(HttpStatusCode.OK, envelope);
        var result = await BuildService(client).GetDeviceEventsAsync(42);

        Assert.Equal(2, result.Total);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("CPU spike", result.Items[0].LogMessage);
    }

    [Fact]
    public async Task GetDeviceEventsAsync_ReturnsEmpty_WhenNoEvents()
    {
        var envelope = new LogicMonitorResponse<LogicMonitorListData<DeviceEvent>>
        {
            Status = 200, ErrorMessage = "OK",
            Data = new LogicMonitorListData<DeviceEvent> { Total = 0, Items = [] }
        };

        using var client = BuildHttpClient(HttpStatusCode.OK, envelope);
        var result = await BuildService(client).GetDeviceEventsAsync(42);

        Assert.Equal(0, result.Total);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetDeviceEventsAsync_Throws_WhenApiReturnsServerError()
    {
        using var client = BuildHttpClient(HttpStatusCode.InternalServerError, null);
        await Assert.ThrowsAsync<HttpRequestException>(() => BuildService(client).GetDeviceEventsAsync(1));
    }

    // ── GetDeviceAlertsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetDeviceAlertsAsync_ReturnsAlerts_WhenSuccessful()
    {
        var items = new[]
        {
            new DeviceAlert { Id = "DS100", MonitorObjectId = 42, Severity = 2, DataSource = "CPU" },
            new DeviceAlert { Id = "DS101", MonitorObjectId = 42, Severity = 4, DataSource = "Disk" }
        };
        var envelope = new LogicMonitorResponse<LogicMonitorListData<DeviceAlert>>
        {
            Status = 200, ErrorMessage = "OK",
            Data = new LogicMonitorListData<DeviceAlert> { Total = 2, Items = items }
        };

        using var client = BuildHttpClient(HttpStatusCode.OK, envelope);
        var result = await BuildService(client).GetDeviceAlertsAsync(42);

        Assert.Equal(2, result.Total);
        Assert.Equal("DS100", result.Items[0].Id);
        Assert.Equal(2, result.Items[0].Severity);
    }

    [Fact]
    public async Task GetDeviceAlertsAsync_ReturnsEmpty_WhenNoAlerts()
    {
        var envelope = new LogicMonitorResponse<LogicMonitorListData<DeviceAlert>>
        {
            Status = 200, ErrorMessage = "OK",
            Data = new LogicMonitorListData<DeviceAlert> { Total = 0, Items = [] }
        };

        using var client = BuildHttpClient(HttpStatusCode.OK, envelope);
        var result = await BuildService(client).GetDeviceAlertsAsync(42);

        Assert.Equal(0, result.Total);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetDeviceAlertsAsync_Throws_WhenApiReturnsServerError()
    {
        using var client = BuildHttpClient(HttpStatusCode.InternalServerError, null);
        await Assert.ThrowsAsync<HttpRequestException>(() => BuildService(client).GetDeviceAlertsAsync(1));
    }

    // ── SearchLogEventsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task SearchLogEventsAsync_ReturnsLogEvents_WhenSuccessful()
    {
        var items = new[]
        {
            new LogEvent { Id = "log-1", Message = "Connection established", Timestamp = "2024-06-01T00:00:00Z" },
            new LogEvent { Id = "log-2", Message = "Timeout occurred",       Timestamp = "2024-06-01T00:01:00Z" }
        };
        var envelope = new LogicMonitorResponse<LogicMonitorListData<LogEvent>>
        {
            Status = 200, ErrorMessage = "OK",
            Data = new LogicMonitorListData<LogEvent> { Total = 2, Items = items }
        };

        using var client = BuildHttpClient(HttpStatusCode.OK, envelope);
        var result = await BuildService(client).SearchLogEventsAsync(filter: "_lm.resourceId.system.deviceId:\"42\"");

        Assert.Equal(2, result.Total);
        Assert.Equal("Connection established", result.Items[0].Message);
    }

    [Fact]
    public async Task SearchLogEventsAsync_ReturnsEmpty_WhenNoLogs()
    {
        var envelope = new LogicMonitorResponse<LogicMonitorListData<LogEvent>>
        {
            Status = 200, ErrorMessage = "OK",
            Data = new LogicMonitorListData<LogEvent> { Total = 0, Items = [] }
        };

        using var client = BuildHttpClient(HttpStatusCode.OK, envelope);
        var result = await BuildService(client).SearchLogEventsAsync();

        Assert.Equal(0, result.Total);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task SearchLogEventsAsync_Throws_WhenApiReturnsServerError()
    {
        using var client = BuildHttpClient(HttpStatusCode.InternalServerError, null);
        await Assert.ThrowsAsync<HttpRequestException>(() => BuildService(client).SearchLogEventsAsync());
    }
}
