using System.Net;
using System.Net.Http.Json;
using LMAPI.Models;

namespace LMAPI.Services;

public class LogicMonitorService : ILogicMonitorService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LogicMonitorService> _logger;

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
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new HttpRequestException(
                "LogicMonitor returned 401 Unauthorized. " +
                "Verify that LogicMonitor:AccessId and LogicMonitor:AccessKey are correct " +
                "and that the API token has not been revoked or expired.");
        response.EnsureSuccessStatusCode();

        var lmResponse = await response.Content
            .ReadFromJsonAsync<LogicMonitorResponse<LogicMonitorListData<Device>>>(
                cancellationToken: cancellationToken);

        return lmResponse?.Data ?? new LogicMonitorListData<Device>();
    }

    /// <inheritdoc/>
    public async Task<Device?> GetDeviceAsync(int deviceId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("LM API v3 → GET /device/devices/{DeviceId}", deviceId);

        var response = await _httpClient.GetAsync($"device/devices/{deviceId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Device {DeviceId} not found in LogicMonitor", deviceId);
            return null;
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new HttpRequestException(
                "LogicMonitor returned 401 Unauthorized. " +
                "Verify that LogicMonitor:AccessId and LogicMonitor:AccessKey are correct " +
                "and that the API token has not been revoked or expired.");

        response.EnsureSuccessStatusCode();

        var lmResponse = await response.Content
            .ReadFromJsonAsync<LogicMonitorResponse<Device>>(cancellationToken: cancellationToken);

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
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new HttpRequestException(
                "LogicMonitor returned 401 Unauthorized. " +
                "Verify that LogicMonitor:AccessId and LogicMonitor:AccessKey are correct " +
                "and that the API token has not been revoked or expired.");
        response.EnsureSuccessStatusCode();

        var lmResponse = await response.Content
            .ReadFromJsonAsync<LogicMonitorResponse<LogicMonitorListData<DeviceEvent>>>(
                cancellationToken: cancellationToken);

        return lmResponse?.Data ?? new LogicMonitorListData<DeviceEvent>();
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
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new HttpRequestException(
                "LogicMonitor returned 401 Unauthorized. " +
                "Verify that LogicMonitor:AccessId and LogicMonitor:AccessKey are correct " +
                "and that the API token has not been revoked or expired.");
        response.EnsureSuccessStatusCode();

        var lmResponse = await response.Content
            .ReadFromJsonAsync<LogicMonitorResponse<LogicMonitorListData<DeviceAlert>>>(
                cancellationToken: cancellationToken);

        return lmResponse?.Data ?? new LogicMonitorListData<DeviceAlert>();
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
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new HttpRequestException(
                "LogicMonitor returned 401 Unauthorized. " +
                "Verify that LogicMonitor:AccessId and LogicMonitor:AccessKey are correct " +
                "and that the API token has not been revoked or expired.");
        response.EnsureSuccessStatusCode();

        var lmResponse = await response.Content
            .ReadFromJsonAsync<LogicMonitorResponse<LogicMonitorListData<LogEvent>>>(
                cancellationToken: cancellationToken);

        return lmResponse?.Data ?? new LogicMonitorListData<LogEvent>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildUrl(string path, int size, int offset, string? filter)
    {
        var query = $"size={size}&offset={offset}";
        if (!string.IsNullOrWhiteSpace(filter))
            query += $"&filter={Uri.EscapeDataString(filter)}";
        return $"{path}?{query}";
    }
}
