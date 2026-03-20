using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LMAPI.Controllers;
using LMAPI.Mcp;
using LMAPI.Models;
using LMAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LMAPI.Tests;

public class McpControllerTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private static McpController BuildController(ILogicMonitorService? service = null)
    {
        service ??= Mock.Of<ILogicMonitorService>();
        return new McpController(service, NullLogger<McpController>.Instance);
    }

    private static JsonRpcRequest MakeRequest(string method, object? @params = null)
    {
        var json = @params is null ? "null" : JsonSerializer.Serialize(@params);
        var paramsElement = JsonSerializer.Deserialize<JsonElement>(json);
        return new JsonRpcRequest
        {
            Method = method,
            Params = paramsElement
        };
    }

    private static JsonRpcResponse GetResponse(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        return JsonSerializer.Deserialize<JsonRpcResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    // ── initialize ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Initialize_ReturnsServerInfoAndCapabilities()
    {
        var controller = BuildController();
        var req = MakeRequest("initialize", new { protocolVersion = "2024-11-05" });

        var actionResult = await controller.HandleAsync(req, default);
        var response = GetResponse(actionResult);

        Assert.Null(response.Error);
        Assert.NotNull(response.Result);

        var resultJson = JsonSerializer.Serialize(response.Result);
        Assert.Contains("protocolVersion", resultJson);
        Assert.Contains("LMAPI", resultJson);
        Assert.Contains("tools", resultJson);
    }

    // ── tools/list ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ToolsList_ReturnsFourTools()
    {
        var controller = BuildController();
        var req = MakeRequest("tools/list");

        var actionResult = await controller.HandleAsync(req, default);
        var response = GetResponse(actionResult);

        Assert.Null(response.Error);

        var resultJson = JsonSerializer.Serialize(response.Result);
        Assert.Contains("get_device", resultJson);
        Assert.Contains("get_device_events", resultJson);
        Assert.Contains("get_device_alerts", resultJson);
        Assert.Contains("search_log_events", resultJson);
    }

    // ── unknown method ────────────────────────────────────────────────────────

    [Fact]
    public async Task UnknownMethod_ReturnsMethodNotFoundError()
    {
        var controller = BuildController();
        var req = MakeRequest("unknown/method");

        var actionResult = await controller.HandleAsync(req, default);
        var response = GetResponse(actionResult);

        Assert.NotNull(response.Error);
        Assert.Equal(-32601, response.Error!.Code);
    }

    // ── tools/call — get_device ───────────────────────────────────────────────

    [Fact]
    public async Task ToolsCall_GetDevice_ReturnsDeviceJson()
    {
        var device = new Device { Id = 42, DisplayName = "Web-Server-01", AlertStatus = "normal" };
        var svcMock = new Mock<ILogicMonitorService>();
        svcMock.Setup(s => s.GetDeviceAsync(42, It.IsAny<CancellationToken>()))
               .ReturnsAsync(device);

        var controller = BuildController(svcMock.Object);
        var req = MakeRequest("tools/call", new
        {
            name = "get_device",
            arguments = new { deviceId = 42 }
        });

        var actionResult = await controller.HandleAsync(req, default);
        var response = GetResponse(actionResult);

        Assert.Null(response.Error);
        var resultJson = JsonSerializer.Serialize(response.Result);
        Assert.Contains("Web-Server-01", resultJson);
        svcMock.Verify(s => s.GetDeviceAsync(42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToolsCall_GetDevice_NotFound_ReturnsNotFoundText()
    {
        var svcMock = new Mock<ILogicMonitorService>();
        svcMock.Setup(s => s.GetDeviceAsync(999, It.IsAny<CancellationToken>()))
               .ReturnsAsync((Device?)null);

        var controller = BuildController(svcMock.Object);
        var req = MakeRequest("tools/call", new
        {
            name = "get_device",
            arguments = new { deviceId = 999 }
        });

        var actionResult = await controller.HandleAsync(req, default);
        var response = GetResponse(actionResult);

        Assert.Null(response.Error);
        var resultJson = JsonSerializer.Serialize(response.Result);
        Assert.Contains("not found", resultJson);
    }

    // ── tools/call — get_device_alerts ────────────────────────────────────────

    [Fact]
    public async Task ToolsCall_GetDeviceAlerts_ReturnsAlertsJson()
    {
        var data = new LogicMonitorListData<DeviceAlert>
        {
            Total = 1,
            Items = [new DeviceAlert { Id = "DS999", Severity = 2, MonitorObjectId = 42 }]
        };

        var svcMock = new Mock<ILogicMonitorService>();
        svcMock.Setup(s => s.GetDeviceAlertsAsync(42, 50, 0, null, It.IsAny<CancellationToken>()))
               .ReturnsAsync(data);

        var controller = BuildController(svcMock.Object);
        var req = MakeRequest("tools/call", new
        {
            name = "get_device_alerts",
            arguments = new { deviceId = 42 }
        });

        var actionResult = await controller.HandleAsync(req, default);
        var response = GetResponse(actionResult);

        Assert.Null(response.Error);
        var resultJson = JsonSerializer.Serialize(response.Result);
        Assert.Contains("DS999", resultJson);
    }

    // ── tools/call — search_log_events ────────────────────────────────────────

    [Fact]
    public async Task ToolsCall_SearchLogEvents_ReturnsLogsJson()
    {
        var data = new LogicMonitorListData<LogEvent>
        {
            Total = 1,
            Items = [new LogEvent { Id = "log-1", Message = "Connection timeout" }]
        };

        var svcMock = new Mock<ILogicMonitorService>();
        svcMock.Setup(s => s.SearchLogEventsAsync(It.IsAny<string?>(), 50, 0, It.IsAny<CancellationToken>()))
               .ReturnsAsync(data);

        var controller = BuildController(svcMock.Object);
        var req = MakeRequest("tools/call", new
        {
            name = "search_log_events",
            arguments = new { filter = "_lm.resourceId.system.deviceId:\"42\"" }
        });

        var actionResult = await controller.HandleAsync(req, default);
        var response = GetResponse(actionResult);

        Assert.Null(response.Error);
        var resultJson = JsonSerializer.Serialize(response.Result);
        Assert.Contains("Connection timeout", resultJson);
    }

    // ── tools/call — error handling ───────────────────────────────────────────

    [Fact]
    public async Task ToolsCall_UnknownTool_ReturnsMethodNotFoundError()
    {
        var controller = BuildController();
        var req = MakeRequest("tools/call", new
        {
            name = "nonexistent_tool",
            arguments = new { }
        });

        var actionResult = await controller.HandleAsync(req, default);
        var response = GetResponse(actionResult);

        Assert.NotNull(response.Error);
        Assert.Equal(-32601, response.Error!.Code);
    }

    [Fact]
    public async Task ToolsCall_LmApiError_ReturnsIsErrorContent()
    {
        var svcMock = new Mock<ILogicMonitorService>();
        svcMock.Setup(s => s.GetDeviceAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new HttpRequestException("Connection refused"));

        var controller = BuildController(svcMock.Object);
        var req = MakeRequest("tools/call", new
        {
            name = "get_device",
            arguments = new { deviceId = 1 }
        });

        var actionResult = await controller.HandleAsync(req, default);
        var response = GetResponse(actionResult);

        // HTTP errors surface as isError:true content, not a JSON-RPC error
        Assert.Null(response.Error);
        var resultJson = JsonSerializer.Serialize(response.Result);
        Assert.Contains("isError", resultJson);
    }
}
