using System.Text.Json.Serialization;

namespace LMAPI.Models;

/// <summary>Outer envelope returned by every LogicMonitor REST API call.</summary>
public class LogicMonitorResponse<T>
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("errmsg")]
    public string ErrorMessage { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}
