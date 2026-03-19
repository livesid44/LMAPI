using System.Text.Json.Serialization;

namespace LMAPI.Models;

/// <summary>
/// Represents a single log event returned by the LogicMonitor REST API v3
/// <c>GET /log/events</c> (LM Logs / Log Intelligence) endpoint.
/// </summary>
public class LogEvent
{
    /// <summary>Unique identifier of the log event.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>ISO 8601 timestamp of the log message.</summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    /// <summary>The raw log message text.</summary>
    [JsonPropertyName("msg")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Resource identifiers used to correlate the log to a monitored resource
    /// (e.g. <c>system.deviceId</c>, <c>system.hostname</c>).
    /// </summary>
    [JsonPropertyName("_lm.resourceId")]
    public Dictionary<string, string> ResourceId { get; set; } = [];

    /// <summary>Additional metadata key/value pairs attached to the log event.</summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = [];
}
