using LMAPI.Models;
using LMAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LMAPI.Controllers;

/// <summary>Proxies LogicMonitor device and event data to API consumers.</summary>
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

    /// <summary>
    /// Returns details for a single LogicMonitor device by its device ID.
    /// </summary>
    /// <param name="id">The LogicMonitor device ID (integer).</param>
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

    /// <summary>
    /// Returns a paged list of events/logs for the specified LogicMonitor device.
    /// </summary>
    /// <param name="id">The LogicMonitor device ID (integer).</param>
    /// <param name="size">Number of events to return (default 50, max 1000).</param>
    /// <param name="offset">Zero-based pagination offset (default 0).</param>
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
        CancellationToken cancellationToken = default)
    {
        if (size < 1 || size > 1000)
            return BadRequest(new { message = "size must be between 1 and 1000." });

        if (offset < 0)
            return BadRequest(new { message = "offset must be 0 or greater." });

        try
        {
            var events = await _logicMonitorService.GetDeviceEventsAsync(id, size, offset, cancellationToken);
            return Ok(events);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching events for device {DeviceId} from LogicMonitor", id);
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "Unable to reach the LogicMonitor API." });
        }
    }
}
