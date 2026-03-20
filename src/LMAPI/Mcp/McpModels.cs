using System.Text.Json;
using System.Text.Json.Serialization;

namespace LMAPI.Mcp;

// ── JSON-RPC 2.0 envelope ─────────────────────────────────────────────────────

public class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public JsonElement? Params { get; set; }
}

public class JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; } = "2.0";

    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; set; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonRpcError? Error { get; set; }
}

public class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; set; }
}

// ── MCP protocol messages ─────────────────────────────────────────────────────

/// <summary>Payload for the MCP <c>initialize</c> method response.</summary>
public class McpInitializeResult
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; } = "2024-11-05";

    [JsonPropertyName("serverInfo")]
    public McpServerInfo ServerInfo { get; set; } = new();

    [JsonPropertyName("capabilities")]
    public McpCapabilities Capabilities { get; set; } = new();
}

public class McpServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "LMAPI";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";
}

public class McpCapabilities
{
    [JsonPropertyName("tools")]
    public object Tools { get; set; } = new { };
}

/// <summary>Payload for the MCP <c>tools/list</c> method response.</summary>
public class McpToolsListResult
{
    [JsonPropertyName("tools")]
    public IReadOnlyList<McpTool> Tools { get; set; } = [];
}

/// <summary>An MCP tool definition (name + description + JSON Schema for input).</summary>
public class McpTool
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("inputSchema")]
    public object InputSchema { get; set; } = new { };
}

/// <summary>Payload for the MCP <c>tools/call</c> method response.</summary>
public class McpToolCallResult
{
    [JsonPropertyName("content")]
    public IReadOnlyList<McpContent> Content { get; set; } = [];

    [JsonPropertyName("isError")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsError { get; set; }
}

public class McpContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

/// <summary>Typed arguments for the <c>tools/call</c> params block.</summary>
public class McpToolCallParams
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public JsonElement? Arguments { get; set; }
}
