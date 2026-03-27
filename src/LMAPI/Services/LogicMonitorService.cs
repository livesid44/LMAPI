using System.Net;
using System.Text.Json;
using LMAPI.Models;

namespace LMAPI.Services;

public class LogicMonitorService : ILogicMonitorService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LogicMonitorService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions =
        new(JsonSerializerDefaults.Web);

    public LogicMonitorService(HttpClient httpClient, ILogger<LogicMonitorService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    // ── Devices ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<LogicMonitorListData<Device>> GetDevicesAsync(
        int size = 50,
        int offset = 0,
        string? filter = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("device/devices", size, offset, filter);
        _logger.LogInformation("LM API v3 → GET {Url}", url);

        var response = await _httpClient.GetAsync(url, cancellationToken);
        var lmResponse = await ReadLmBodyAsync<LogicMonitorListData<Device>>(response, url, cancellationToken);
        var result = lmResponse?.Data ?? new LogicMonitorListData<Device>();
        result.LmStatus  = lmResponse?.Status ?? 0;
        result.LmMessage = lmResponse?.ErrorMessage ?? string.Empty;
        if (result.Total == 0)
            _logger.LogInformation(
                "LM API returned 0 devices for {Url} (LM status={LmStatus}, errmsg=\"{LmMsg}\")",
                url, result.LmStatus, result.LmMessage);
        return result;
    }

    /// <inheritdoc/>
    public async Task<Device?> GetDeviceAsync(int deviceId, CancellationToken cancellationToken = default)
    {
        var url = $"device/devices/{deviceId}";
        _logger.LogInformation("LM API v3 → GET /device/devices/{DeviceId}", deviceId);

        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Device {DeviceId} not found in LogicMonitor", deviceId);
            return null;
        }

        var lmResponse = await ReadLmBodyAsync<Device>(response, url, cancellationToken);
        return lmResponse?.Data;
    }

    // ── Device Events ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<LogicMonitorListData<DeviceEvent>> GetDeviceEventsAsync(
        int deviceId,
        int size = 50,
        int offset = 0,
        string? filter = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"device/devices/{deviceId}/events", size, offset, filter);
        _logger.LogInformation("LM API v3 → GET {Url}", url);

        var response = await _httpClient.GetAsync(url, cancellationToken);
        var lmResponse = await ReadLmBodyAsync<LogicMonitorListData<DeviceEvent>>(response, url, cancellationToken);
        var result = lmResponse?.Data ?? new LogicMonitorListData<DeviceEvent>();
        result.LmStatus  = lmResponse?.Status ?? 0;
        result.LmMessage = lmResponse?.ErrorMessage ?? string.Empty;
        if (result.Total == 0)
            _logger.LogInformation(
                "LM API returned 0 events for device {DeviceId} at {Url} (LM status={LmStatus}, errmsg=\"{LmMsg}\")",
                deviceId, url, result.LmStatus, result.LmMessage);
        return result;
    }

    // ── Alerts ────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<LogicMonitorListData<DeviceAlert>> GetDeviceAlertsAsync(
        int deviceId,
        int size = 50,
        int offset = 0,
        string? filter = null,
        CancellationToken cancellationToken = default)
    {
        // LM v3 filter for a specific device: monitorObjectId:{id}
        var deviceFilter = $"monitorObjectId:{deviceId}";
        var combinedFilter = string.IsNullOrWhiteSpace(filter)
            ? deviceFilter
            : $"{deviceFilter},{filter}";

        var url = BuildUrl("alert/alerts", size, offset, combinedFilter);
        _logger.LogInformation("LM API v3 → GET {Url}", url);

        var response = await _httpClient.GetAsync(url, cancellationToken);
        var lmResponse = await ReadLmBodyAsync<LogicMonitorListData<DeviceAlert>>(response, url, cancellationToken);
        var result = lmResponse?.Data ?? new LogicMonitorListData<DeviceAlert>();
        result.LmStatus  = lmResponse?.Status ?? 0;
        result.LmMessage = lmResponse?.ErrorMessage ?? string.Empty;
        if (result.Total == 0)
            _logger.LogInformation(
                "LM API returned 0 alerts for device {DeviceId} at {Url} (LM status={LmStatus}, errmsg=\"{LmMsg}\")",
                deviceId, url, result.LmStatus, result.LmMessage);
        return result;
    }

    // ── LM Logs / Log Intelligence ────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<LogicMonitorListData<LogEvent>> SearchLogEventsAsync(
        string? filter = null,
        int size = 50,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("log/events", size, offset, filter);
        _logger.LogInformation("LM API v3 → GET {Url}", url);

        var response = await _httpClient.GetAsync(url, cancellationToken);
        var lmResponse = await ReadLmBodyAsync<LogicMonitorListData<LogEvent>>(response, url, cancellationToken);
        var result = lmResponse?.Data ?? new LogicMonitorListData<LogEvent>();
        result.LmStatus  = lmResponse?.Status ?? 0;
        result.LmMessage = lmResponse?.ErrorMessage ?? string.Empty;
        if (result.Total == 0)
            _logger.LogInformation(
                "LM API returned 0 log events for {Url} (LM status={LmStatus}, errmsg=\"{LmMsg}\")",
                url, result.LmStatus, result.LmMessage);
        return result;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildUrl(string path, int size, int offset, string? filter)
    {
        var query = $"size={size}&offset={offset}";
        if (!string.IsNullOrWhiteSpace(filter))
            query += $"&filter={Uri.EscapeDataString(filter)}";
        return $"{path}?{query}";
    }

    /// <summary>
    /// Reads and deserializes the body of an LM API <see cref="HttpResponseMessage"/>
    /// after the per-method status-code pre-checks have been applied.
    /// <list type="bullet">
    ///   <item>Throws <see cref="HttpRequestException"/> on HTTP 401.</item>
    ///   <item>Calls <see cref="HttpResponseMessage.EnsureSuccessStatusCode"/> for other non-2xx responses.</item>
    ///   <item>Reads the raw body as a string and logs it at Debug level.</item>
    ///   <item>Deserializes with <see cref="JsonSerializerDefaults.Web"/> options; translates
    ///         <see cref="JsonException"/> into <see cref="HttpRequestException"/>.</item>
    ///   <item>Calls <see cref="EnsureLmSuccess{T}"/> to detect LM application-level errors.</item>
    ///   <item>Logs a Warning when the response envelope carries no data.</item>
    /// </list>
    /// </summary>
    private async Task<LogicMonitorResponse<T>?> ReadLmBodyAsync<T>(
        HttpResponseMessage response,
        string urlForLogging,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new HttpRequestException(
                "LogicMonitor returned 401 Unauthorized. " +
                "Verify that LogicMonitor:BearerToken is correct " +
                "and that the token has not been revoked or expired.");

        response.EnsureSuccessStatusCode();

        var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogDebug("LM API raw response for {Url}: {Body}", urlForLogging, rawBody);

        LogicMonitorResponse<T>? lmResponse;
        try
        {
            lmResponse = JsonSerializer.Deserialize<LogicMonitorResponse<T>>(rawBody, _jsonOptions);
        }
        catch (JsonException ex)
        {
            throw new HttpRequestException(
                $"LogicMonitor returned malformed JSON: {ex.Message}", ex);
        }

        EnsureLmSuccess(lmResponse);

        if (lmResponse is null)
        {
            _logger.LogWarning(
                "LM API response for {Url} deserialised to null — raw body: {Body}",
                urlForLogging, rawBody);
        }
        else if ((object?)lmResponse.Data == null)
        {
            _logger.LogWarning(
                "LM API returned HTTP 200 (LM status={LmStatus}, errmsg=\"{LmMsg}\") " +
                "but the 'data' field was absent or null for {Url} — raw body: {Body}",
                lmResponse.Status, lmResponse.ErrorMessage, urlForLogging, rawBody);
        }

        return lmResponse;
    }

    /// <summary>
    /// Validates a deserialized LogicMonitor response envelope and throws an
    /// <see cref="HttpRequestException"/> if the LM application-level error code
    /// indicates a failure.
    /// <para>
    /// LogicMonitor v3 can return HTTP 200 with an error payload in the body — for
    /// example <c>{ "errorCode": 1401, "errorMessage": "Authentication failed" }</c>
    /// or <c>{ "status": 1401, "errmsg": "Authentication failed" }</c>.
    /// This method detects both shapes.
    /// </para>
    /// </summary>
    private static void EnsureLmSuccess<T>(LogicMonitorResponse<T>? lmResponse)
    {
        if (lmResponse is null) return;

        // Determine the effective LM error code: prefer errorCode (the newer field),
        // fall back to status.  Status == 200 (or 0 when not present) means success.
        var code = lmResponse.ErrorCode != 0 ? lmResponse.ErrorCode
                 : lmResponse.Status      != 0 ? lmResponse.Status
                 : 0;

        if (code is 0 or 200) return;

        // Prefer the more descriptive message when both fields are present.
        var message = !string.IsNullOrWhiteSpace(lmResponse.ErrorMessageAlt)
            ? lmResponse.ErrorMessageAlt
            : lmResponse.ErrorMessage;

        if (code == 1401)
            throw new HttpRequestException(
                $"LogicMonitor authentication failed (errorCode {code}: {message}). " +
                "Verify that LogicMonitor:BearerToken is correct " +
                "and that the token has not been revoked or expired.");

        throw new HttpRequestException(
            $"LogicMonitor returned error {code}: {message}");
    }
}
