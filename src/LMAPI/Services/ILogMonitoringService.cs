using LMAPI.Models;

namespace LMAPI.Services;

public interface ILogMonitoringService
{
    Task<LogQueryResponse> GetLogsAsync(LogQueryRequest request, CancellationToken cancellationToken = default);
    Task<LogEntry?> GetLogByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> PostLogAsync(LogEntry entry, CancellationToken cancellationToken = default);
}
