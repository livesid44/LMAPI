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
    // ── helpers ─────────────────────────────────────────────────────────────

    private static HttpClient BuildHttpClient(
        HttpStatusCode statusCode,
        object? responseBody)
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

    // ── GetDeviceAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetDeviceAsync_ReturnsDevice_WhenFound()
    {
        var expected = new Device { Id = 42, DisplayName = "Server-01", Name = "192.168.1.1", Status = "normal" };
        var lmResponse = new LogicMonitorResponse<Device> { Status = 200, ErrorMessage = "OK", Data = expected };

        using var client = BuildHttpClient(HttpStatusCode.OK, lmResponse);
        var sut = new LogicMonitorService(client, NullLogger<LogicMonitorService>.Instance);

        var device = await sut.GetDeviceAsync(42);

        Assert.NotNull(device);
        Assert.Equal(42, device.Id);
        Assert.Equal("Server-01", device.DisplayName);
    }

    [Fact]
    public async Task GetDeviceAsync_ReturnsNull_WhenNotFound()
    {
        using var client = BuildHttpClient(HttpStatusCode.NotFound, null);
        var sut = new LogicMonitorService(client, NullLogger<LogicMonitorService>.Instance);

        var device = await sut.GetDeviceAsync(999);

        Assert.Null(device);
    }

    [Fact]
    public async Task GetDeviceAsync_Throws_WhenApiReturnsServerError()
    {
        using var client = BuildHttpClient(HttpStatusCode.InternalServerError, null);
        var sut = new LogicMonitorService(client, NullLogger<LogicMonitorService>.Instance);

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetDeviceAsync(1));
    }

    // ── GetDeviceEventsAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetDeviceEventsAsync_ReturnsEvents_WhenSuccessful()
    {
        var items = new[]
        {
            new DeviceEvent { Id = "e1", DeviceId = 42, LogMessage = "CPU spike", Severity = "warning" },
            new DeviceEvent { Id = "e2", DeviceId = 42, LogMessage = "Disk full",  Severity = "error" }
        };

        var lmResponse = new LogicMonitorResponse<LogicMonitorListData<DeviceEvent>>
        {
            Status = 200,
            ErrorMessage = "OK",
            Data = new LogicMonitorListData<DeviceEvent> { Total = 2, Items = items }
        };

        using var client = BuildHttpClient(HttpStatusCode.OK, lmResponse);
        var sut = new LogicMonitorService(client, NullLogger<LogicMonitorService>.Instance);

        var result = await sut.GetDeviceEventsAsync(42);

        Assert.Equal(2, result.Total);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("CPU spike", result.Items[0].LogMessage);
    }

    [Fact]
    public async Task GetDeviceEventsAsync_ReturnsEmpty_WhenNoEvents()
    {
        var lmResponse = new LogicMonitorResponse<LogicMonitorListData<DeviceEvent>>
        {
            Status = 200,
            ErrorMessage = "OK",
            Data = new LogicMonitorListData<DeviceEvent> { Total = 0, Items = [] }
        };

        using var client = BuildHttpClient(HttpStatusCode.OK, lmResponse);
        var sut = new LogicMonitorService(client, NullLogger<LogicMonitorService>.Instance);

        var result = await sut.GetDeviceEventsAsync(42);

        Assert.Equal(0, result.Total);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetDeviceEventsAsync_Throws_WhenApiReturnsServerError()
    {
        using var client = BuildHttpClient(HttpStatusCode.InternalServerError, null);
        var sut = new LogicMonitorService(client, NullLogger<LogicMonitorService>.Instance);

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetDeviceEventsAsync(1));
    }
}
