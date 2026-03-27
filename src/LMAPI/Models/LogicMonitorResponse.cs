using System.Text.Json.Serialization;

namespace LMAPI.Models;

/// <summary>
/// Outer envelope returned by every LogicMonitor REST API v3 call.
/// <para>
/// Successful responses use the shape:
/// <c>{ "status": 200, "errmsg": "OK", "data": {...} }</c>
/// </para>
/// <para>
/// Application-level errors (e.g. authentication failure) may be returned with
/// HTTP 200 but a non-success payload in one of two shapes:
/// <list type="bullet">
///   <item><c>{ "status": 1401, "errmsg": "Authentication failed" }</c></item>
///   <item><c>{ "errorCode": 1401, "errorMessage": "Authentication failed", "errorDetail": null }</c></item>
/// </list>
/// Both shapes are captured so callers can detect and surface the error.
/// </para>
/// </summary>
public class LogicMonitorResponse<T>
{
    /// <summary>LM v3 status code (200 = success; non-200 = error).</summary>
    [JsonPropertyName("status")]
    public int Status { get; set; }

    /// <summary>Short error message returned in the <c>errmsg</c> field.</summary>
    [JsonPropertyName("errmsg")]
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// LM application-level error code returned in the <c>errorCode</c> field
    /// (used by some error responses instead of / in addition to <c>status</c>).
    /// Zero when not present.
    /// </summary>
    [JsonPropertyName("errorCode")]
    public int ErrorCode { get; set; }

    /// <summary>
    /// Human-readable error message from the <c>errorMessage</c> field
    /// (used by some error responses; distinct from <c>errmsg</c>).
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessageAlt { get; set; }

    /// <summary>Additional error detail returned in the <c>errorDetail</c> field.</summary>
    [JsonPropertyName("errorDetail")]
    public string? ErrorDetail { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}
