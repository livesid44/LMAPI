using System.Text.Json.Serialization;

namespace LMAPI.Models;

/// <summary>Paged list wrapper returned by LogicMonitor list endpoints.</summary>
public class LogicMonitorListData<T>
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("items")]
    public IReadOnlyList<T> Items { get; set; } = [];
}
