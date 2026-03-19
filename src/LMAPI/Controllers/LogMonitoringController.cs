using LMAPI.Models;
using LMAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LMAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LogMonitoringController : ControllerBase
{
    private readonly ILogMonitoringService _logMonitoringService;
    private readonly ILogger<LogMonitoringController> _logger;

    public LogMonitoringController(
        ILogMonitoringService logMonitoringService,
        ILogger<LogMonitoringController> logger)
    {
        _logMonitoringService = logMonitoringService;
        _logger = logger;
    }

    /// <summary>
    /// Queries log entries from the log monitoring API.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(LogQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetLogs([FromQuery] LogQueryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _logMonitoringService.GetLogsAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling log monitoring API");
            return StatusCode(StatusCodes.Status502BadGateway, "Unable to reach the log monitoring API.");
        }
    }

    /// <summary>
    /// Retrieves a single log entry by its ID from the log monitoring API.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(LogEntry), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetLogById(string id, CancellationToken cancellationToken)
    {
        try
        {
            var entry = await _logMonitoringService.GetLogByIdAsync(id, cancellationToken);
            if (entry is null)
                return NotFound();

            return Ok(entry);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling log monitoring API for id {Id}", id);
            return StatusCode(StatusCodes.Status502BadGateway, "Unable to reach the log monitoring API.");
        }
    }

    /// <summary>
    /// Forwards a log entry to the log monitoring API.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> PostLog([FromBody] LogEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _logMonitoringService.PostLogAsync(entry, cancellationToken);
            if (success)
                return Accepted();

            return StatusCode(StatusCodes.Status502BadGateway, "Log monitoring API rejected the entry.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error posting log entry to log monitoring API");
            return StatusCode(StatusCodes.Status502BadGateway, "Unable to reach the log monitoring API.");
        }
    }
}
