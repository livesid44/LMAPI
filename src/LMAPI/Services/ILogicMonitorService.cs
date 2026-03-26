using LMAPI.Models;

namespace LMAPI.Services;

public interface ILogicMonitorService
{
    // ── Devices ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a paged list of all devices in the LogicMonitor organization from
    /// <c>GET /device/devices</c> (LM REST API v3).
    /// </summary>
    /// <param name="size">Page size (1–1000, default 50).</param>
    /// <param name="offset">Zero-based page offset (default 0).</param>
    /// <param name="filter">
    /// Optional LM v3 filter expression, e.g. <c>alertStatus:"critical"</c>
    /// or <c>displayName~"web"</c> (contains match).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<LogicMonitorListData<Device>> GetDevicesAsync(
        int size = 50,
        int offset = 0,
        string? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns full details for a single device from
    /// <c>GET /device/devices/{id}</c> (LM REST API v3).
    /// </summary>
    Task<Device?> GetDeviceAsync(int deviceId, CancellationToken cancellationToken = default);

    // ── Device Events ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a paged list of events/logs for the specified device from
    /// <c>GET /device/devices/{id}/events</c> (LM REST API v3).
    /// </summary>
    /// <param name="deviceId">LogicMonitor device ID.</param>
    /// <param name="size">Page size (1–1000, default 50).</param>
    /// <param name="offset">Zero-based page offset (default 0).</param>
    /// <param name="filter">Optional LM filter expression, e.g. <c>severity:"error"</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<LogicMonitorListData<DeviceEvent>> GetDeviceEventsAsync(
        int deviceId,
        int size = 50,
        int offset = 0,
        string? filter = null,
        CancellationToken cancellationToken = default);

    // ── Alerts ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a paged list of alerts for the specified device from
    /// <c>GET /alert/alerts?filter=monitorObjectId:{id}</c> (LM REST API v3).
    /// </summary>
    /// <param name="deviceId">LogicMonitor device ID.</param>
    /// <param name="size">Page size (1–1000, default 50).</param>
    /// <param name="offset">Zero-based page offset (default 0).</param>
    /// <param name="filter">
    /// Optional additional LM filter expression to AND with the device filter,
    /// e.g. <c>severity:2</c> for critical only.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<LogicMonitorListData<DeviceAlert>> GetDeviceAlertsAsync(
        int deviceId,
        int size = 50,
        int offset = 0,
        string? filter = null,
        CancellationToken cancellationToken = default);

    // ── LM Logs / Log Intelligence ────────────────────────────────────────────

    /// <summary>
    /// Searches log events in LM Logs from
    /// <c>GET /log/events</c> (LM REST API v3).
    /// </summary>
    /// <param name="filter">LM filter expression (e.g. <c>_lm.resourceId.system.deviceId:"42"</c>).</param>
    /// <param name="size">Page size (1–1000, default 50).</param>
    /// <param name="offset">Zero-based page offset (default 0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<LogicMonitorListData<LogEvent>> SearchLogEventsAsync(
        string? filter = null,
        int size = 50,
        int offset = 0,
        CancellationToken cancellationToken = default);
}
