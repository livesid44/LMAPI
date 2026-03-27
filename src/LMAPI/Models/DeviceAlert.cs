using System.Text.Json.Serialization;

namespace LMAPI.Models;

/// <summary>
/// Represents an active or historical alert returned by the LogicMonitor REST API v3
/// <c>GET /alert/alerts</c> endpoint, filtered by <c>monitorObjectId</c>.
/// </summary>
public class DeviceAlert
{
    /// <summary>Unique alert ID (e.g. "DS12345678").</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Alert type: "dataSource", "website", "agentDown", etc.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Human-readable alert type label.</summary>
    [JsonPropertyName("typeLabel")]
    public string TypeLabel { get; set; } = string.Empty;

    /// <summary>Severity level: 2 = critical, 3 = error, 4 = warning.</summary>
    [JsonPropertyName("severity")]
    public int Severity { get; set; }

    /// <summary>Whether the alert has been acknowledged.</summary>
    [JsonPropertyName("acked")]
    public bool Acked { get; set; }

    [JsonPropertyName("ackedBy")]
    public string AckedBy { get; set; } = string.Empty;

    /// <summary>Unix epoch (seconds) when the alert was acknowledged.</summary>
    [JsonPropertyName("ackedOn")]
    public long AckedOn { get; set; }

    [JsonPropertyName("ackedOnLocal")]
    public string AckedOnLocal { get; set; } = string.Empty;

    /// <summary>Alert rule that triggered this alert.</summary>
    [JsonPropertyName("rule")]
    public string Rule { get; set; } = string.Empty;

    [JsonPropertyName("chain")]
    public string Chain { get; set; } = string.Empty;

    /// <summary>Unix epoch (seconds) when the alert started.</summary>
    [JsonPropertyName("startEpoch")]
    public long StartEpoch { get; set; }

    [JsonPropertyName("startEpochLocal")]
    public string StartEpochLocal { get; set; } = string.Empty;

    /// <summary>Unix epoch (seconds) when the alert ended (0 = still active).</summary>
    [JsonPropertyName("endEpoch")]
    public long EndEpoch { get; set; }

    [JsonPropertyName("endEpochLocal")]
    public string EndEpochLocal { get; set; } = string.Empty;

    /// <summary>ID of the device (monitor object) that triggered the alert.</summary>
    [JsonPropertyName("monitorObjectId")]
    public int MonitorObjectId { get; set; }

    [JsonPropertyName("monitorObjectName")]
    public string MonitorObjectName { get; set; } = string.Empty;

    [JsonPropertyName("monitorObjectType")]
    public string MonitorObjectType { get; set; } = string.Empty;

    [JsonPropertyName("dataSource")]
    public string DataSource { get; set; } = string.Empty;

    [JsonPropertyName("dataSourceId")]
    public int DataSourceId { get; set; }

    [JsonPropertyName("dataSourceInstance")]
    public string DataSourceInstance { get; set; } = string.Empty;

    [JsonPropertyName("dataPoint")]
    public string DataPoint { get; set; } = string.Empty;

    /// <summary>The metric value that triggered the alert.</summary>
    [JsonPropertyName("alertValue")]
    public string AlertValue { get; set; } = string.Empty;

    /// <summary>The threshold expression that was breached.</summary>
    [JsonPropertyName("threshold")]
    public string Threshold { get; set; } = string.Empty;

    [JsonPropertyName("inSDT")]
    public bool InSdt { get; set; }

    [JsonPropertyName("sdted")]
    public bool Sdted { get; set; }

    [JsonPropertyName("note")]
    public string Note { get; set; } = string.Empty;
}
