using LMAPI.Models;
using LMAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LMAPI.Controllers;

/// <summary>
/// Exposes LM Logs (Log Intelligence) search capability,
/// proxying the LM REST API v3 <c>/log/events</c> endpoint.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LogsController : ControllerBase
{
    private readonly ILogicMonitorService _logicMonitorService;
    private readonly ILogger<LogsController> _logger;

    public LogsController(
        ILogicMonitorService logicMonitorService,
        ILogger<LogsController> logger)
    {
        _logicMonitorService = logicMonitorService;
        _logger = logger;
    }

    // ── GET /api/logs/events ──────────────────────────────────────────────────

    /// <summary>
    /// Searches log events in LM Logs (Log Intelligence).
    /// Internally calls <c>GET /log/events</c> on the LM REST API v3.
    /// </summary>
    /// <param name="filter">
    /// LM v3 filter expression, e.g.
    /// <c>_lm.resourceId.system.deviceId:"42"</c> to scope logs to a specific device.
    /// </param>
    /// <param name="size">Number of log events to return (1–1000, default 50).</param>
    /// <param name="offset">Zero-based pagination offset (default 0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Log events returned successfully.</response>
    /// <response code="400">Invalid query parameters.</response>
    /// <response code="502">Unable to reach the LogicMonitor API.</response>
    [HttpGet("events")]
    [ProducesResponseType(typeof(LogicMonitorListData<LogEvent>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetLogEvents(
        [FromQuery] string? filter = null,
        [FromQuery] int size = 50,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        if (size is < 1 or > 1000)
            return BadRequest(new { message = "size must be between 1 and 1000." });
        if (offset < 0)
            return BadRequest(new { message = "offset must be 0 or greater." });

        try
        {
            var logs = await _logicMonitorService
                .SearchLogEventsAsync(filter, size, offset, cancellationToken);
            return Ok(logs);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error searching log events in LogicMonitor");
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "Unable to reach the LogicMonitor API." });
        }
    }
}
