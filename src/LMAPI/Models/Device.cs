using System.Text.Json.Serialization;

namespace LMAPI.Models;

/// <summary>Represents a monitored device (host/resource) in LogicMonitor.</summary>
public class Device
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("hostGroupIds")]
    public string HostGroupIds { get; set; } = string.Empty;

    [JsonPropertyName("currentCollectorId")]
    public int CurrentCollectorId { get; set; }

    [JsonPropertyName("deviceType")]
    public int DeviceType { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("upTimeInSeconds")]
    public long UpTimeInSeconds { get; set; }

    [JsonPropertyName("lastDataTime")]
    public long LastDataTime { get; set; }

    [JsonPropertyName("updatedOn")]
    public long UpdatedOn { get; set; }

    [JsonPropertyName("createdOn")]
    public long CreatedOn { get; set; }
}
