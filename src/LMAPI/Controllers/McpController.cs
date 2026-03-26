using System.Text.Json;
using LMAPI.Mcp;
using LMAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LMAPI.Controllers;

/// <summary>
/// Implements a Model Context Protocol (MCP) server over HTTP using JSON-RPC 2.0.
/// <para>
/// Supported methods:
/// <list type="bullet">
///   <item><term>initialize</term><description>Returns server info and capabilities.</description></item>
///   <item><term>tools/list</term><description>Returns the tool manifest.</description></item>
///   <item><term>tools/call</term><description>Invokes a named tool and returns text content.</description></item>
/// </list>
/// </para>
/// <para>
/// Endpoint: <c>POST /mcp</c><br/>
/// Content-Type: <c>application/json</c>
/// </para>
/// </summary>
[ApiController]
[Route("mcp")]
[Produces("application/json")]
public class McpController : ControllerBase
{
    // Standard JSON-RPC 2.0 error codes
    private const int ParseError     = -32700;
    private const int InvalidRequest = -32600;
    private const int MethodNotFound = -32601;
    private const int InvalidParams  = -32602;
    private const int InternalError  = -32603;

    private readonly ILogicMonitorService _lmService;
    private readonly ILogger<McpController> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public McpController(ILogicMonitorService lmService, ILogger<McpController> logger)
    {
        _lmService = lmService;
        _logger = logger;
    }

    /// <summary>
    /// MCP JSON-RPC 2.0 dispatcher. Accepts <c>initialize</c>, <c>tools/list</c>,
    /// and <c>tools/call</c> method calls.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(JsonRpcResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> HandleAsync(
        [FromBody] JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("MCP request: method={Method}", request.Method);

        return request.Method switch
        {
            "initialize"  => Ok(Success(request.Id, new McpInitializeResult())),
            "tools/list"  => Ok(Success(request.Id, new McpToolsListResult { Tools = McpToolDefinitions.All })),
            "tools/call"  => Ok(await DispatchToolCallAsync(request, cancellationToken)),
            _             => Ok(ErrorResponse(request.Id, MethodNotFound, $"Unknown method: {request.Method}"))
        };
    }

    // ── Tool call dispatcher ──────────────────────────────────────────────────

    private async Task<JsonRpcResponse> DispatchToolCallAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Params is null)
            return ErrorResponse(request.Id, InvalidParams, "params is required for tools/call.");

        McpToolCallParams? callParams;
        try
        {
            callParams = request.Params.Value.Deserialize<McpToolCallParams>(_jsonOptions);
        }
        catch (JsonException ex)
        {
            return ErrorResponse(request.Id, ParseError, $"Failed to parse params: {ex.Message}");
        }

        if (callParams is null || string.IsNullOrWhiteSpace(callParams.Name))
            return ErrorResponse(request.Id, InvalidParams, "params.name is required.");

        try
        {
            var result = callParams.Name switch
            {
                "list_devices"      => await ListDevicesAsync(callParams, cancellationToken),
                "get_device"        => await GetDeviceAsync(callParams, cancellationToken),
                "get_device_events" => await GetDeviceEventsAsync(callParams, cancellationToken),
                "get_device_alerts" => await GetDeviceAlertsAsync(callParams, cancellationToken),
                "search_log_events" => await SearchLogEventsAsync(callParams, cancellationToken),
                _                   => null
            };

            if (result is null)
                return ErrorResponse(request.Id, MethodNotFound, $"Unknown tool: {callParams.Name}");

            return Success(request.Id, result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "LogicMonitor API error during MCP tool call {Tool}", callParams.Name);
            return Success(request.Id, new McpToolCallResult
            {
                IsError = true,
                Content = [new McpContent { Text = $"LogicMonitor API error: {ex.Message}" }]
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during MCP tool call {Tool}", callParams.Name);
            return ErrorResponse(request.Id, InternalError, "An unexpected error occurred.");
        }
    }

    // ── Tool implementations ──────────────────────────────────────────────────

    private async Task<McpToolCallResult> ListDevicesAsync(
        McpToolCallParams callParams,
        CancellationToken cancellationToken)
    {
        var size   = GetOptionalInt(callParams.Arguments, "size",   50);
        var offset = GetOptionalInt(callParams.Arguments, "offset", 0);
        var filter = GetOptionalString(callParams.Arguments, "filter");

        var data = await _lmService.GetDevicesAsync(size, offset, filter, cancellationToken);
        return TextResult(JsonSerializer.Serialize(data, _jsonOptions));
    }

    private async Task<McpToolCallResult> GetDeviceAsync(
        McpToolCallParams callParams,
        CancellationToken cancellationToken)
    {
        var deviceId = GetRequiredInt(callParams.Arguments, "deviceId");
        var device = await _lmService.GetDeviceAsync(deviceId, cancellationToken);

        if (device is null)
            return TextResult($"Device {deviceId} was not found in LogicMonitor.");

        return TextResult(JsonSerializer.Serialize(device, _jsonOptions));
    }

    private async Task<McpToolCallResult> GetDeviceEventsAsync(
        McpToolCallParams callParams,
        CancellationToken cancellationToken)
    {
        var deviceId = GetRequiredInt(callParams.Arguments, "deviceId");
        var size     = GetOptionalInt(callParams.Arguments, "size",   50);
        var offset   = GetOptionalInt(callParams.Arguments, "offset", 0);
        var filter   = GetOptionalString(callParams.Arguments, "filter");

        var data = await _lmService.GetDeviceEventsAsync(deviceId, size, offset, filter, cancellationToken);
        return TextResult(JsonSerializer.Serialize(data, _jsonOptions));
    }

    private async Task<McpToolCallResult> GetDeviceAlertsAsync(
        McpToolCallParams callParams,
        CancellationToken cancellationToken)
    {
        var deviceId = GetRequiredInt(callParams.Arguments, "deviceId");
        var size     = GetOptionalInt(callParams.Arguments, "size",   50);
        var offset   = GetOptionalInt(callParams.Arguments, "offset", 0);
        var filter   = GetOptionalString(callParams.Arguments, "filter");

        var data = await _lmService.GetDeviceAlertsAsync(deviceId, size, offset, filter, cancellationToken);
        return TextResult(JsonSerializer.Serialize(data, _jsonOptions));
    }

    private async Task<McpToolCallResult> SearchLogEventsAsync(
        McpToolCallParams callParams,
        CancellationToken cancellationToken)
    {
        var filter = GetOptionalString(callParams.Arguments, "filter");
        var size   = GetOptionalInt(callParams.Arguments, "size",   50);
        var offset = GetOptionalInt(callParams.Arguments, "offset", 0);

        var data = await _lmService.SearchLogEventsAsync(filter, size, offset, cancellationToken);
        return TextResult(JsonSerializer.Serialize(data, _jsonOptions));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static JsonRpcResponse Success(JsonElement? id, object result) =>
        new() { Id = id, Result = result };

    private static JsonRpcResponse ErrorResponse(JsonElement? id, int code, string message) =>
        new() { Id = id, Error = new JsonRpcError { Code = code, Message = message } };

    private static McpToolCallResult TextResult(string text) =>
        new() { Content = [new McpContent { Text = text }] };

    private static int GetRequiredInt(JsonElement? args, string key)
    {
        if (args is null || !args.Value.TryGetProperty(key, out var el))
            throw new InvalidOperationException($"Required argument '{key}' is missing.");

        return el.ValueKind == JsonValueKind.Number
            ? el.GetInt32()
            : throw new InvalidOperationException($"Argument '{key}' must be an integer.");
    }

    private static int GetOptionalInt(JsonElement? args, string key, int defaultValue)
    {
        if (args is null || !args.Value.TryGetProperty(key, out var el))
            return defaultValue;

        return el.ValueKind == JsonValueKind.Number ? el.GetInt32() : defaultValue;
    }

    private static string? GetOptionalString(JsonElement? args, string key)
    {
        if (args is null || !args.Value.TryGetProperty(key, out var el))
            return null;

        return el.ValueKind == JsonValueKind.String ? el.GetString() : null;
    }
}
