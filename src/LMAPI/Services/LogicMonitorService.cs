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

    /// <inheritdoc/>
    public async Task<Device?> GetDeviceAsync(int deviceId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching device {DeviceId} from LogicMonitor", deviceId);

        var response = await _httpClient.GetAsync($"device/devices/{deviceId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Device {DeviceId} not found in LogicMonitor", deviceId);
            return null;
        }

        response.EnsureSuccessStatusCode();

        var lmResponse = await response.Content
            .ReadFromJsonAsync<LogicMonitorResponse<Device>>(cancellationToken: cancellationToken);

        return lmResponse?.Data;
    }

    /// <inheritdoc/>
    public async Task<LogicMonitorListData<DeviceEvent>> GetDeviceEventsAsync(
        int deviceId,
        int size = 50,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Fetching events for device {DeviceId} from LogicMonitor (size={Size}, offset={Offset})",
            deviceId, size, offset);

        var url = $"device/devices/{deviceId}/events?size={size}&offset={offset}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var lmResponse = await response.Content
            .ReadFromJsonAsync<LogicMonitorResponse<LogicMonitorListData<DeviceEvent>>>(
                cancellationToken: cancellationToken);

        return lmResponse?.Data ?? new LogicMonitorListData<DeviceEvent>();
    }
}
