using LMAPI.Models;
using LMAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LMAPI.Controllers;

/// <summary>
/// Exposes LogicMonitor device details and device-level events,
/// proxying the LM REST API v3 <c>/device/devices</c> endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DevicesController : ControllerBase
{
    private readonly ILogicMonitorService _logicMonitorService;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(
        ILogicMonitorService logicMonitorService,
        ILogger<DevicesController> logger)
    {
        _logicMonitorService = logicMonitorService;
        _logger = logger;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Appends <c>X-LM-Status</c> and <c>X-LM-Message</c> response headers so
    /// callers can see what LogicMonitor returned when the result set is empty.
    /// </summary>
    private void AddLmDiagnosticHeaders(int lmStatus, string lmMessage)
    {
        if (lmStatus != 0)
            Response.Headers.Append("X-LM-Status", lmStatus.ToString());
        if (!string.IsNullOrEmpty(lmMessage))
            Response.Headers.Append("X-LM-Message", lmMessage);
    }

    // ── GET /api/devices ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns a paged list of all devices in the LogicMonitor organization.
    /// Internally calls <c>GET /device/devices</c> on the LM REST API v3.
    /// </summary>
    /// <param name="size">Number of devices to return (1–1000, default 50).</param>
    /// <param name="offset">Zero-based pagination offset (default 0).</param>
    /// <param name="filter">
    /// Optional LM v3 filter expression, e.g. <c>alertStatus:"critical"</c>
    /// or <c>displayName~"web"</c> (contains match).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Device list returned successfully.</response>
    /// <response code="400">Invalid query parameters.</response>
    /// <response code="502">Unable to reach the LogicMonitor API.</response>
    [HttpGet]
    [ProducesResponseType(typeof(LogicMonitorListData<Device>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetDevices(
        [FromQuery] int size = 50,
        [FromQuery] int offset = 0,
        [FromQuery] string? filter = null,
        CancellationToken cancellationToken = default)
    {
        if (size is < 1 or > 1000)
            return BadRequest(new { message = "size must be between 1 and 1000." });
        if (offset < 0)
            return BadRequest(new { message = "offset must be 0 or greater." });

        try
        {
            var devices = await _logicMonitorService
                .GetDevicesAsync(size, offset, filter, cancellationToken);
            if (devices.Total == 0)
                AddLmDiagnosticHeaders(devices.LmStatus, devices.LmMessage);
            return Ok(devices);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching device list from LogicMonitor");
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "Unable to reach the LogicMonitor API." });
        }
    }

    // ── GET /api/devices/{id} ────────────────────────────────────────────────

    /// <summary>
    /// Returns full details for a LogicMonitor device by its device ID.
    /// Internally calls <c>GET /device/devices/{id}</c> on the LM REST API v3.
    /// </summary>
    /// <param name="id">The LogicMonitor integer device ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Device details returned successfully.</response>
    /// <response code="404">No device exists with the given ID.</response>
    /// <response code="502">Unable to reach the LogicMonitor API.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Device), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetDevice(int id, CancellationToken cancellationToken)
    {
        try
        {
            var device = await _logicMonitorService.GetDeviceAsync(id, cancellationToken);
            if (device is null)
                return NotFound(new { message = $"Device {id} not found." });

            return Ok(device);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching device {DeviceId} from LogicMonitor", id);
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "Unable to reach the LogicMonitor API." });
        }
    }

    // ── GET /api/devices/{id}/events ─────────────────────────────────────────

    /// <summary>
    /// Returns a paged list of events/logs for a LogicMonitor device.
    /// Internally calls <c>GET /device/devices/{id}/events</c> on the LM REST API v3.
    /// </summary>
    /// <param name="id">The LogicMonitor integer device ID.</param>
    /// <param name="size">Number of events to return (1–1000, default 50).</param>
    /// <param name="offset">Zero-based pagination offset (default 0).</param>
    /// <param name="filter">
    /// Optional LM v3 filter expression applied server-side, e.g. <c>severity:"error"</c>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Events returned successfully.</response>
    /// <response code="400">Invalid query parameters.</response>
    /// <response code="502">Unable to reach the LogicMonitor API.</response>
    [HttpGet("{id:int}/events")]
    [ProducesResponseType(typeof(LogicMonitorListData<DeviceEvent>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetDeviceEvents(
        int id,
        [FromQuery] int size = 50,
        [FromQuery] int offset = 0,
        [FromQuery] string? filter = null,
        CancellationToken cancellationToken = default)
    {
        if (size is < 1 or > 1000)
            return BadRequest(new { message = "size must be between 1 and 1000." });
        if (offset < 0)
            return BadRequest(new { message = "offset must be 0 or greater." });

        try
        {
            var events = await _logicMonitorService
                .GetDeviceEventsAsync(id, size, offset, filter, cancellationToken);
            if (events.Total == 0)
                AddLmDiagnosticHeaders(events.LmStatus, events.LmMessage);
            return Ok(events);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching events for device {DeviceId} from LogicMonitor", id);
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "Unable to reach the LogicMonitor API." });
        }
    }

    // ── GET /api/devices/{id}/alerts ─────────────────────────────────────────

    /// <summary>
    /// Returns a paged list of alerts for a LogicMonitor device.
    /// Internally calls <c>GET /alert/alerts?filter=monitorObjectId:{id}</c> on the LM REST API v3.
    /// </summary>
    /// <param name="id">The LogicMonitor integer device ID.</param>
    /// <param name="size">Number of alerts to return (1–1000, default 50).</param>
    /// <param name="offset">Zero-based pagination offset (default 0).</param>
    /// <param name="filter">
    /// Optional additional LM v3 filter expression (ANDed with the device filter),
    /// e.g. <c>severity:2</c> for critical alerts only.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Alerts returned successfully.</response>
    /// <response code="400">Invalid query parameters.</response>
    /// <response code="502">Unable to reach the LogicMonitor API.</response>
    [HttpGet("{id:int}/alerts")]
    [ProducesResponseType(typeof(LogicMonitorListData<DeviceAlert>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetDeviceAlerts(
        int id,
        [FromQuery] int size = 50,
        [FromQuery] int offset = 0,
        [FromQuery] string? filter = null,
        CancellationToken cancellationToken = default)
    {
        if (size is < 1 or > 1000)
            return BadRequest(new { message = "size must be between 1 and 1000." });
        if (offset < 0)
            return BadRequest(new { message = "offset must be 0 or greater." });

        try
        {
            var alerts = await _logicMonitorService
                .GetDeviceAlertsAsync(id, size, offset, filter, cancellationToken);
            if (alerts.Total == 0)
                AddLmDiagnosticHeaders(alerts.LmStatus, alerts.LmMessage);
            return Ok(alerts);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching alerts for device {DeviceId} from LogicMonitor", id);
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "Unable to reach the LogicMonitor API." });
        }
    }
}
