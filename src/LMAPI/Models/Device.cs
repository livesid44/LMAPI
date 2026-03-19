using System.Text.Json.Serialization;

namespace LMAPI.Models;

/// <summary>
/// Represents a monitored device (host/resource) in LogicMonitor.
/// Field names match the LogicMonitor REST API v3 <c>GET /device/devices/{id}</c> response.
/// </summary>
public class Device
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>The DNS name or IP address used by the collector to communicate with the device.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Human-readable display name shown in the LogicMonitor UI.</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Comma-separated list of device group IDs the device belongs to.</summary>
    [JsonPropertyName("hostGroupIds")]
    public string HostGroupIds { get; set; } = string.Empty;

    [JsonPropertyName("currentCollectorId")]
    public int CurrentCollectorId { get; set; }

    [JsonPropertyName("currentCollectorDescription")]
    public string CurrentCollectorDescription { get; set; } = string.Empty;

    /// <summary>0 = regular device, 2 = AWS device, 4 = Azure device, etc.</summary>
    [JsonPropertyName("deviceType")]
    public int DeviceType { get; set; }

    /// <summary>Current alert/operational status of the device (e.g. "normal", "warn", "error", "critical").</summary>
    [JsonPropertyName("alertStatus")]
    public string AlertStatus { get; set; } = string.Empty;

    [JsonPropertyName("upTimeInSeconds")]
    public long UpTimeInSeconds { get; set; }

    /// <summary>Unix epoch (seconds) of the last data point received.</summary>
    [JsonPropertyName("lastDataTime")]
    public long LastDataTime { get; set; }

    [JsonPropertyName("updatedOn")]
    public long UpdatedOn { get; set; }

    [JsonPropertyName("createdOn")]
    public long CreatedOn { get; set; }

    [JsonPropertyName("disableAlerting")]
    public bool DisableAlerting { get; set; }

    [JsonPropertyName("enableNetflow")]
    public bool EnableNetflow { get; set; }

    [JsonPropertyName("link")]
    public string Link { get; set; } = string.Empty;

    /// <summary>Custom properties defined on the device.</summary>
    [JsonPropertyName("customProperties")]
    public IReadOnlyList<DeviceProperty> CustomProperties { get; set; } = [];

    /// <summary>System-assigned auto-properties (e.g. system.os, system.ips).</summary>
    [JsonPropertyName("autoProperties")]
    public IReadOnlyList<DeviceProperty> AutoProperties { get; set; } = [];

    [JsonPropertyName("inheritedProperties")]
    public IReadOnlyList<DeviceProperty> InheritedProperties { get; set; } = [];
}

/// <summary>A name/value pair representing a LogicMonitor device property.</summary>
public class DeviceProperty
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}
