using System.Text.Json.Serialization;

namespace LMAPI.Models;

/// <summary>Represents a single device event (alert/log) from LogicMonitor.</summary>
public class DeviceEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("happenedOn")]
    public long HappenedOn { get; set; }

    [JsonPropertyName("happenedOnLocal")]
    public string HappenedOnLocal { get; set; } = string.Empty;

    [JsonPropertyName("logMessage")]
    public string LogMessage { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

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
}
