using System.Net.Http.Json;
using System.Web;
using LMAPI.Models;

namespace LMAPI.Services;

public class LogMonitoringService : ILogMonitoringService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LogMonitoringService> _logger;

    public LogMonitoringService(HttpClient httpClient, ILogger<LogMonitoringService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<LogQueryResponse> GetLogsAsync(
        LogQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);

        if (request.From.HasValue)
            query["from"] = request.From.Value.ToString("o");
        if (request.To.HasValue)
            query["to"] = request.To.Value.ToString("o");
        if (!string.IsNullOrWhiteSpace(request.Level))
            query["level"] = request.Level;
        if (!string.IsNullOrWhiteSpace(request.Source))
            query["source"] = request.Source;
        if (!string.IsNullOrWhiteSpace(request.SearchText))
            query["searchText"] = request.SearchText;

        query["page"] = request.Page.ToString();
        query["pageSize"] = request.PageSize.ToString();

        var url = $"logs?{query}";

        _logger.LogInformation("Fetching logs from log monitoring API: {Url}", url);

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LogQueryResponse>(cancellationToken: cancellationToken);
        return result ?? new LogQueryResponse();
    }

    public async Task<LogEntry?> GetLogByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching log entry {Id} from log monitoring API", id);

        var response = await _httpClient.GetAsync($"logs/{Uri.EscapeDataString(id)}", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<LogEntry>(cancellationToken: cancellationToken);
    }

    public async Task<bool> PostLogAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Posting log entry to log monitoring API");

        var response = await _httpClient.PostAsJsonAsync("logs", entry, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
