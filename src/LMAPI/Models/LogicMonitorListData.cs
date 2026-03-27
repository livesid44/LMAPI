using System.Text.Json.Serialization;

namespace LMAPI.Models;

/// <summary>Paged list wrapper returned by LogicMonitor list endpoints.</summary>
public class LogicMonitorListData<T>
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("items")]
    public IReadOnlyList<T> Items { get; set; } = [];

    // ── Diagnostic metadata ────────────────────────────────────────────────────
    // These fields are NOT serialized to the JSON response body (JsonIgnore).
    // They carry the LM envelope status/errmsg so controllers can surface them as
    // HTTP response headers (X-LM-Status / X-LM-Message) for debugging.

    /// <summary>
    /// The LogicMonitor response envelope <c>status</c> or <c>errorCode</c> value.
    /// Populated by <see cref="Services.LogicMonitorService"/>; not included in the
    /// JSON response body.
    /// </summary>
    [JsonIgnore]
    public int LmStatus { get; set; }

    /// <summary>
    /// The LogicMonitor response envelope <c>errmsg</c> or <c>errorMessage</c> value.
    /// Populated by <see cref="Services.LogicMonitorService"/>; not included in the
    /// JSON response body.
    /// </summary>
    [JsonIgnore]
    public string LmMessage { get; set; } = string.Empty;
}
