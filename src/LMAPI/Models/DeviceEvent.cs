using System.Text.Json.Serialization;

namespace LMAPI.Models;

/// <summary>
/// Represents a single device event returned by the LogicMonitor REST API v3
/// <c>GET /device/devices/{id}/events</c> endpoint.
/// </summary>
public class DeviceEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Unix epoch (seconds) when the event occurred.</summary>
    [JsonPropertyName("happenedOn")]
    public long HappenedOn { get; set; }

    /// <summary>Local date/time string of when the event occurred.</summary>
    [JsonPropertyName("happenedOnLocal")]
    public string HappenedOnLocal { get; set; } = string.Empty;

    [JsonPropertyName("logMessage")]
    public string LogMessage { get; set; } = string.Empty;

    /// <summary>Event type (e.g. "alert", "agentDownAlert", "hostStatusChange").</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Severity level: "error", "warn", "critical", "clear".</summary>
    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    [JsonPropertyName("deviceId")]
    public int DeviceId { get; set; }

    [JsonPropertyName("deviceDisplayName")]
    public string DeviceDisplayName { get; set; } = string.Empty;

    [JsonPropertyName("instanceId")]
    public int InstanceId { get; set; }

    [JsonPropertyName("instanceName")]
    public string InstanceName { get; set; } = string.Empty;

    [JsonPropertyName("dataPointName")]
    public string DataPointName { get; set; } = string.Empty;

    [JsonPropertyName("ackComment")]
    public string AckComment { get; set; } = string.Empty;

    [JsonPropertyName("inSDT")]
    public bool InSdt { get; set; }
}
